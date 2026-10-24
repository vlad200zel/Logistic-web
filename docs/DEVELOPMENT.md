# Разработка LogisticsWeb

Техническая документация для того, кто будет менять код: окружение, сборка, устройство проекта и типовые задачи.

## 1. Требования

| Инструмент | Версия | Зачем |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/8.0) | 8.0+ | сборка и запуск |
| Git | любая | работа с репозиторием |
| IDE | Visual Studio 2022 / VS Code / Rider | разработка |

Visual Studio: открыть `LogisticsWeb.sln`. VS Code: подойдёт расширение C# Dev Kit.

## 2. Сборка и запуск

```bash
dotnet restore                     # восстановить зависимости
dotnet build                       # собрать
dotnet run                         # запустить (http://localhost:5085)
dotnet run --launch-profile https  # запустить с HTTPS (https://localhost:7237)
dotnet watch run                   # горячая перезагрузка при правках
```

Профили запуска заданы в `Properties/launchSettings.json`.

**Особенности запуска:**

- каталог `App_Data` создаётся автоматически при первом старте;
- `app.UseHttpsRedirection()` включён, поэтому при запуске по `http` браузер может перенаправлять на порт HTTPS — используйте профиль `http` из `launchSettings.json` либо профиль `https`;
- `UseSession()` должен вызываться до `UseAuthorization()` — иначе `HttpContext.Session` будет `null`;
- для карты и OSRM нужен доступ в интернет; без него расчёт работает на хаверсине, а карта останется серой.

## 3. Структура кода

```
Program.cs                     — DI, сессии, middleware
LogisticsWeb.csproj            — net8.0, Nullable, ImplicitUsings
Data/ClientRepository.cs       — IClientRepository + JsonClientRepository
Models/Client.cs               — доменная модель клиента
Models/AddressBook.cs          — справочник адресов Москвы (адрес, lat, lon)
Pages/Index.cshtml(.cs)        — расчёт маршрута и карта
Pages/Clients.cshtml(.cs)      — CRUD клиентов и автодополнение
Pages/Shared/_Layout.cshtml    — общий макет и навигация
wwwroot/                       — css/js/lib (bootstrap, jquery, leaflet подключается с CDN)
App_Data/clients.json          — данные (демо-набор в репозитории)
```

### Соглашения

- страницы — Razor Pages, логика в `*.cshtml.cs` (`OnGet*`, `OnPost*`);
- зависимости внедряются через конструктор (`IClientRepository` → `ClientsModel`, `IndexModel`);
- в моделях включены `Nullable` и `ImplicitUsings` — не отключайте их в `.csproj`;
- комментарии в расчётном модуле — по-русски, интерфейс — по-русски.

## 4. Типовые задачи

### Добавить поле клиенту

1. `Models/Client.cs` — свойство.
2. `Pages/Clients.cshtml` — `<input asp-for="InputClient.NewField" …>` в форме добавления и в строке таблицы.
3. `Pages/Clients.cshtml.cs` — присвоение в `OnPostUpdateAsync`.
4. Старые записи в `clients.json` без поля десериализуются со значением по умолчанию — миграция не нужна.

### Добавить страницу

1. Создайте `Pages/NewPage.cshtml` и `Pages/NewPage.cshtml.cs` с классом, наследующим `PageModel`.
2. Зарегистрируйте пункт меню в `Pages/Shared/_Layout.cshtml`.
3. Отдельная регистрация в `Program.cs` не нужна: `MapRazorPages()` подхватывает файлы автоматически.

### Изменить стоимость километра

Константа `CostPerKm` в `Pages/Index.cshtml.cs`. Если значение должно меняться из интерфейса — вынесите в `appsettings.json` и читайте через `IConfiguration`.

### Заменить JSON-хранилище на базу данных

1. Подключите EF Core (`Microsoft.EntityFrameworkCore.Sqlite`).
2. Реализуйте `IClientRepository` поверх `DbContext` — интерфейс уже описывает все нужные операции.
3. Замените регистрацию в `Program.cs`:

```csharp
builder.Services.AddScoped<IClientRepository, EfClientRepository>();
```

Страницы при этом не меняются — это и было целью интерфейса.

### Вынести расчёт в отдельный сервис

Сейчас алгоритм живёт в `IndexModel`. Для тестируемости:

1. Создайте `Services/IRouteOptimizer` с методом `Optimize(points, distances)`.
2. Перенесите `FindMostProfitableRouteDijkstra`, `SearchRoutesDijkstra`, `DijkstraShortestPath`.
3. Зарегистрируйте в DI и внедрите в `IndexModel`.
4. Покройте unit-тестами на заранее заданных матрицах расстояний (без обращения к сети).

### Ускорить расчёт

- заменить N² вызовов `/route` на один запрос OSRM `/table`;
- добавить `IMemoryCache` для матрицы расстояний (координаты как ключ);
- ограничить перебор: отсечение по верхней оценке прибыли, 2-opt, OR-Tools для больших наборов.

## 5. Отладка

- **Логи:** консоль `dotnet run`; уровень — в `appsettings.Development.json`.
- **Точки:** сериализованный набор лежит в сессии; удобно логировать в `SavePointsToSession`.
- **Сеть:** запросы к OSRM можно проверить вручную:

```bash
curl "https://router.project-osrm.org/route/v1/driving/37.618,55.751;37.617,55.760?overview=false"
```

- **Ошибки в JS карты:** DevTools браузера, консоль (`Ошибка отрисовки трека OSRM` — сеть или лимит OSRM).

## 6. Что стоит улучшить в первую очередь

| Задача | Польза |
|---|---|
| Unit-тесты для `IRouteOptimizer` | защита от регрессий в главном алгоритме |
| OSRM `/table` + кэш | расчёт быстрее в разы |
| EF Core + SQLite | корректная работа с данными, миграции |
| Ограничения (время смены, вместимость) | приближение к реальной задаче VRP |
| CI (GitHub Actions: `dotnet build`) | проверка сборки на каждый push |

## 7. Полезные ссылки

- [Razor Pages](https://learn.microsoft.com/aspnet/core/razor-pages/)
- [Session в ASP.NET Core](https://learn.microsoft.com/aspnet/core/fundamentals/app-state)
- [OSRM API](https://project-osrm.org/docs/v5.24.0/api/)
- [Leaflet](https://leafletjs.com/reference.html)
