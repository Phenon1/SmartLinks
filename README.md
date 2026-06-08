# SmartLinks

Система выполняет редирект по умной ссылке `/s/{code}`. Пользовательский запрос принимает `RedirectService`, а решение о том, какой URL выбрать, принимает отдельный `RulesService`.

Правила редиректа читаются из JSON-файлов в `RulesService/Rules`, а условия правил загружаются из DLL-плагинов в `RulesService/Plugins`. Сейчас подключен базовый DLL-плагин `RulePlugins.Core` с условиями `time`, `country`, `device` и `browser`.

## Архитектурное описание

Подробное описание для домашнего задания находится в [ARCHITECTURE.md](ARCHITECTURE.md): стиль архитектуры, бизнес-процессы, функциональные процессы, компонентная и информационная схемы, шаблоны и интеграции.

## Запуск

```powershell
dotnet run --project RulesService/RulesService.csproj
dotnet run --project RedirectService/RedirectService.csproj
```

В Docker запускаются два сервиса:

```powershell
docker compose up --build
```

- `redirectservice`: `http://localhost:8080`
- `rulesservice`: `http://localhost:18081`

Пример перехода:

```text
GET http://localhost:8080/s/promo?country=RU
User-Agent: Mozilla/5.0 (iPhone) Safari/604.1
```

В Swagger не стоит нажимать Execute на `GET /s/{code}`: это настоящий редирект, и браузерный `fetch` в Swagger UI может упереться в CORS после перехода на внешний URL. Для проверки правил в Swagger используйте `GET /s/{code}/resolve` - он возвращает JSON-решение без редиректа.

## Архитектура

- `RedirectService` содержит MVC-контроллер `SmartLinksController`.
- Контроллер принимает `/s/{code}`, собирает снимок исходного HTTP-запроса и отправляет его в `RulesService`.
- Общий контракт между сервисами лежит в `SmartLinks.Contracts`.
- `RulesService` содержит движок правил и endpoint `POST /rules/handle`.
- Все условия реализуют общий интерфейс `IRuleCondition`.
- Все условия получают единый вход `RuleHttpContext`: code, время, path, query, headers, ip, country, device, browser.

Настоящий `HttpContext` не передается между сервисами напрямую, потому что он привязан к ASP.NET pipeline конкретного приложения. Вместо этого `RedirectService` отправляет DTO-снимок исходного запроса, а `RulesService` восстанавливает из него единый контекст правил.

## JSON DSL и плагины

Правила хранятся в JSON-файлах папки `RulesService/Rules`. JSON описывает ссылки, порядок правил, `TargetUrl` и параметры условий. `TargetUrl`, `FallbackUrl` и `DefaultUrl` могут быть абсолютными URL или относительными путями. Относительные пути собираются с `Rules:BaseUrl` из `RulesService/appsettings.json`.

```json
{
  "Type": "device",
  "Parameters": {
    "is": "mobile,tablet"
  }
}
```

Поддержанные условия из `RulePlugins.Core`: `time`, `country`, `device`, `browser`.

Новые ссылки и правила добавляются без изменения кода отдельными JSON-файлами в `RulesService/Rules/*.json`. `RulesService` держит каталог правил в памяти и сбрасывает кеш, когда в папке `Rules` появляются изменения. Пример:

```json
{
  "Links": [
    {
      "Code": "promo",
      "Rules": [
        {
          "Name": "russia-mobile-morning",
          "TargetUrl": "/ru-morning",
          "Conditions": [
            {
              "Type": "country",
              "Parameters": {
                "is": "RU"
              }
            },
            {
              "Type": "device",
              "Parameters": {
                "is": "mobile,tablet"
              }
            }
          ]
        }
      ]
    }
  ]
}
```

DLL-плагины в `RulesService/Plugins/*.dll` добавляют типы условий. DLL должна ссылаться на `SmartLinks.Contracts` и реализовать `IRuleCondition`. `ConditionRegistry` держит условия в памяти и сбрасывает кеш при изменениях в папке `Plugins`.

JSON-правило может использовать DLL-условие по `Type`:

```json
{
  "Type": "browser",
  "Parameters": {
    "contains": "Chrome"
  }
}
```

Итого: JSON отвечает за состав правил, DLL отвечает за новое поведение проверки.

Пример базового адреса в конфиге:

```json
{
  "Rules": {
    "BaseUrl": "https://example.com",
    "DefaultUrl": "/"
  }
}
```

## Паттерны и сложность

- Specification: каждое условие правила реализует `IRuleCondition`.
- Chain of Responsibility: правила проверяются по порядку до первого совпадения.
- DI: зависимости регистрируются через `AddSmartLinks` и `AddRulesEngine`.
- Options pattern: настройки читаются через `RulesOptions` и `SmartLinkOptions`.
- DTO/Contract pattern: `SmartLinks.Contracts` задает контракт между сервисами.
- Dynamic Plugin pattern: DLL-файлы в `RulesService/Plugins` добавляют новые условия без изменения `RulesService`.
- Cached Repository pattern: JSON-каталог правил хранится в памяти и перечитывается только после изменений в `RulesService/Rules`.
- Комбинаторный рост правил сдерживается JSON DSL и единым интерфейсом условий.

## Тесты и покрытие

```powershell
dotnet test RedirectService.Tests/Tests.csproj --collect:"XPlat Code Coverage"
```

Текущий результат: 26 тестов.
