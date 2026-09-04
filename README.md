# 植物精灵开发仓库

本仓库管理《植物精灵》Unity 项目、策划文档和配套文档工具。Unity 工程位于 `植物精灵/`，编辑器版本为 Unity 2022.3.62f3。

## 当前状态

当前可交付基线是可运行、可构建、可完整通关的 GGJ48H Demo。它已经覆盖角色控制、战斗、三段 Encounter、三种嫁接、Portal、结算、重开与死亡流程，但尚未达到 V1.1 根门推进版的内容规模。

- 开发交接状态：[`植物精灵/DEVELOPMENT_STATUS.md`](植物精灵/DEVELOPMENT_STATUS.md)
- 里程碑与任务清单：[`PROJECT_PLAN.md`](PROJECT_PLAN.md)
- 运行和操作说明：[`植物精灵/README.md`](植物精灵/README.md)
- 产品与程序规格：[`策划文档/`](策划文档/)

## 仓库结构

- `植物精灵/Assets`：正式游戏代码、场景、数据、美术与测试。
- `植物精灵/Packages`、`植物精灵/ProjectSettings`：Unity 依赖与项目配置。
- `策划文档/`：当前策划案和程序实现规格。
- `tools/`：策划文档生成工具。
- `植物精灵/Archive`：旧原型，仅供参考，不作为正式运行入口。

Unity 的 `Library`、`Logs`、`UserSettings`、`Build` 等可再生成内容不进入版本控制。

## 开发约定

`main` 始终保持可编译和已验证。每次只处理 `PROJECT_PLAN.md` 中一个带编号的工作项；功能分支使用 `feature/ps-编号-简述`，修复分支使用 `fix/ps-编号-简述`。提交前至少运行与改动直接相关的测试，影响主流程时还要执行完整 EditMode 测试和成品冒烟验证。

提交信息采用 `类型: 内容`，例如 `feat: add persistent audio settings`、`fix: prevent portal input during transition`、`docs: update milestone status`。

