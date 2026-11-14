# TASK-003: Настроить принудительное соблюдение TAUSIK для GigaCode

- **Status**: [`x`]  [`done`]
- **Opened**: 2026-11-14
- **Closed**: 2026-11-14
- **Related**: TASK-001, TASK-002
- **TAUSIK**: task-003

## Краткое описание задачи

GigaCode работал без механизмов принуждения TAUSIK: был развёрнут только профиль `claude`, каталога `.qwen/` не существовало, `tausik doctor` показывал покрытие лишь для claude. Команда `python bootstrap/bootstrap.py` из [`AGENTS.md`](../../AGENTS.md) не работала — каталога `bootstrap/` в корне не было, исходник лежал в сабмодуле `.tausik-lib`. Гейт `bootstrap_drift` был красным и неисправимым документированным способом, гейты были python-ориентированы при C#-стеке.

Задача разворачивает профиль GigaCode, чинит точку входа bootstrap, объявляет стек `dotnet` и фиксирует запреты жизненного цикла в правилах хостов.

## Критерии готовности (Definition of Done)

1. Развёрнут `.qwen/settings.json` с MCP и хуками, `QWEN.md` создан, `tausik doctor` показывает покрытие qwen
2. `python bootstrap/bootstrap.py --check` завершается с кодом 0 из корня проекта
3. `tausik verify` проходит без FAIL по `bootstrap_drift`
4. В `.tausik/stacks/dotnet` объявлен стек `dotnet` с гейтом `dotnet-build` (block, verify); `dotnet-test` объявлен и выключен до появления тестовых проектов; `pytest` остаётся stack-scoped для python и к dotnet-стеку не применяется
5. `AGENTS.md` и `QWEN.md` содержат запреты: ручная правка статуса задачи, `--no-file-changes` при изменённых файлах, коммит до verify, неполное AC-доказательство
6. [`src/FinamClient/FinamClient.csproj`](../../src/FinamClient/FinamClient.csproj) использует `ProjectReference`, `restore` и `pack` проходят на чистой копии с пустым кэшем пакетов
7. `task-001` и `task-002` имеют статус `done` в БД TAUSIK
8. Закрытие задачи без зелёного verify receipt отклоняется CLI

## Негативные сценарии (Negative Scenarios)

1. Профиль развёрнут, но хост не читает его конфиг — правила остаются декларацией, гейты не срабатывают
2. `bootstrap --ide all` разворачивает шесть профилей по ~10 МБ и оставляет рабочее дерево грязным после каждого verify
3. `--no-file-changes` при уже закоммиченных изменениях скрывает область от git-проверки и пропускает гейты
4. `ProjectReference`, заменённый на `PackageReference`, ломает сборку с чистого клона (`NU1101`)
5. `dotnet-test` включён при отсутствии тестовых проектов — зелёный результат без проверки

## НЕ ТРОГАТЬ (NOT to touch)

1. Никогда не править файлы скриптов и файлы конфигурации за пределом каталога решения
2. Не трогать код приложения ([`src/FinamClient/FinamClient.cs`](../../src/FinamClient/FinamClient.cs), [`src/FinamClient/FinamApiException.cs`](../../src/FinamClient/FinamApiException.cs), [`src/FinamClient/FinamOptions.cs`](../../src/FinamClient/FinamOptions.cs))
3. Не трогать [`samples/`](../../samples/) и [`src/Finam.TradeApi.Grpc/proto/`](../../src/Finam.TradeApi.Grpc/proto/) (сгенерированные и внешние файлы)
