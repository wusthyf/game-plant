# 植物精灵 GGJ48H Demo

使用 Unity 2022.3.62f3 打开工程。正式场景顺序为 `MainMenu`、`Level01`，Windows 成品位于 `Build/Windows/PlantSpirit.exe`。

## 操作

- `A` / `D` 或左右方向键：移动
- `Space`：跳跃
- `Left Shift`：冲刺
- 鼠标左键或 `J`：普通攻击
- 鼠标右键或 `K`：技能
- `Tab` 或 `G`：打开嫁接面板
- `1` / `2` / `3`：装备根、茎、花部件
- `E`：进入已开启的传送门
- `Esc`：关闭嫁接面板或切换暂停

暂停、死亡和结算面板均可使用鼠标按钮操作。每次重新挑战都会清空库存、装备和本局计时。

## 构建

在 Unity 中使用 `Plant Spirit/Build Formal Demo Scenes` 重建正式场景。批处理 Release 构建入口为 `PlantSpirit.GGJ.Editor.GGJBuild.BuildWindows64`。

## 已知范围

当前版本已接入项目方提供的主角攻击、藤蔓怪、蘑菇、孢子/命中特效和地下遗迹场景资源，并接入 15 个 CC0 音效与主音量/音乐/音效设置。甲虫、主角移动状态、Portal、嫁接图标和 UI 仍使用可替换的程序表现；候选背景音乐因缺少逐首授权说明暂未接入。P0 主流程、角色控制、战斗、三种敌人、三个 Encounter、三种嫁接、Portal 与完整 UI 状态已接入。
