# TASK-002: Локальный nuget-пакет

- **Status**: [`x`]  [`done`]
- **Opened**: 2026-11-10
- **Closed**: 2026-11-14
- **Related**: TASK-001
- **TAUSIK**: task-002

## Краткое описание задачи

Настроить сборку и публикацию nuget-пакетов для проектов решения [`FinamClient.slnx`](../../FinamClient.slnx) и опубликовать их в локальный nuget-репозиторий, чтобы библиотеку `FinamClient` можно было подключать в другие проекты через `PackageReference`.

Пакетов должно быть два, т.к. проект [`src/FinamClient/FinamClient.csproj`](../../src/FinamClient/FinamClient.csproj) ссылается на проект [`src/Finam.TradeApi.Grpc/Finam.TradeApi.Grpc.csproj`](../../src/Finam.TradeApi.Grpc/Finam.TradeApi.Grpc.csproj):

1. `Finam.TradeApi.Grpc` — сгенерированный gRPC-клиент Finam Trade API (net8.0; зависимости `Google.Protobuf 3.28.3`, `Grpc.Net.Client 2.67.0`, `Grpc.Tools 2.67.0`)
2. `FinamClient` — высокоуровневая обёртка, в пакете должна сохраниться зависимость от пакета `Finam.TradeApi.Grpc`

## Критерии готовности (Definition of Done)

1. Оба упаковываемых проекта (`FinamClient`, `Finam.TradeApi.Grpc`) содержат корректные nuget-метаданные: `PackageId`, `Version`, `Authors`, `Description`, `License`/`PackageLicenseExpression`, `RepositoryUrl` и т.п. (в csproj нет предупреждений вида `NU5105`/отсутствующих метаданных при `dotnet pack`)
2. В корне решения есть источник для локального nuget-репозитория (локальный каталог-фид), добавленный стандартным для dotnet способом (`dotnet nuget add source` или `NuGet.config` в каталоге решения)
3. Пакеты собираются стандартным для dotnet способом: `dotnet pack -c Release` создаёт `.nupkg` для обоих проектов
4. Пакеты публикуются в локальный nuget-репозиторий стандартным для dotnet способом: `dotnet nuget push <nupkg> --source <local-feed>` завершается успешно, пакеты видны в локальном фиде
5. Пакет `FinamClient` содержит dependency на `Finam.TradeApi.Grpc` (проверить через `dotnet nuget why` / содержимое `.nuspec` внутри `.nupkg`)
6. Восстановление работает из локального фида: в отдельном тестовом проекте `PackageReference` на `FinamClient` резолвится из локального репозитория без подключения `ProjectReference` (`dotnet restore` без ошибок)
7. В README.md присутствует инструкция по сборке nuget-пакета
8. В README.md присутствует инструкция по подключению nuget-пакета к другому проекту 

## Негативные сценарии (Negative Scenarios)

1. Пакет `FinamClient` собран без пакета `Finam.TradeApi.Grpc` — при установке `FinamClient` в другой проект восстановление падает (неразрешимая зависимость)
2. В пакеты не заданы/дублируются `PackageId` или `Version` — `dotnet pack` предупреждения, конфликт версий при повторном `push` (nuget отклоняет перезапись существующей версии)
3. Публикация в несуществующий/недоступный локальный источник — `dotnet nuget push` завершается ошибкой, пакет не появляется в фиде
4. При сборке gRPC-проекта в пакет не попадают protobuf-ассеты или не восстанавливаются `Google.Protobuf`/`Grpc.Net.Client` — потребитель пакета получает ошибку компиляции/выполнения
5. Случайное попадание в пакет лишнего содержимого (samples, task-файлы, секреты) — проверить состав `.nupkg` перед публикацией
6. Чувствительные данные (токены, ключи) попали в `.nupkg` или в `NuGet.config` с кредами

## НЕ ТРОГАТЬ (NOT to touch)

1. Никогда не править файлы скриптов и файлы конфигурации за пределом каталога решения
2. Не трогать код приложения
3. Не трогать [`samples/`](../../samples/) и [`src/Finam.TradeApi.Grpc/proto/`](../../src/Finam.TradeApi.Grpc/proto/) (сгенерированные и внешние файлы)
