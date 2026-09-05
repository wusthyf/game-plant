# 背包 UI 美术资源包（B：幽光植物炼金室）

本目录包含 21 张由 image-2 生成并为 Unity 整理的 PNG。展示图和背景为不透明成品；可复用控件、角色、物品和 VFX 图集均为 RGBA 透明资源。

## 目录

- `Previews`：空背包、已获得、选中铁甲根、三件全装备，共 4 张完整状态预览。
- `Structure`：月夜森林背景、完整 UI 外壳、结构面板图集、汁液管线连接图集。
- `Controls`：按钮、六边形装备槽、物品格、效果徽章、分类/状态徽章、焦点覆盖层。
- `Items`：铁甲根、藤蔓触须、毒菌伞的图标、标本瓶、装备态、装备覆盖层和拾取通知。
- `Characters`：角色的 8 种装备组合。
- `VFX`：根、茎、花各 8 帧的嫁接/装备反馈动画。

## 图集顺序

所有顺序均为“从左到右、从上到下”。Unity 自动切片后的后缀 `_00`、`_01`……与下列顺序一致。

| 文件 | 网格 | 顺序 |
| --- | --- | --- |
| `inventory-b-button-states.png` | 1×5 | 普通、悬停、按下、选中、禁用 |
| `inventory-b-slot-states.png` | 1×5 | 空、悬停、选中、已装备、锁定 |
| `inventory-b-cell-states.png` | 3×2 | 空、悬停、选中、已获得、已装备、锁定 |
| `inventory-b-items-atlas.png` | 3×3 | 第一行独立图标；第二行标本瓶；第三行装备发光态。每行依次为铁甲根、藤蔓触须、毒菌伞 |
| `inventory-b-notification-banners.png` | 1×3 | 获得铁甲根、获得藤蔓触须、获得毒菌伞 |
| `inventory-b-equipment-overlays.png` | 3×1 | 根部护甲、藤蔓手臂、毒菌伞帽 |
| `inventory-b-character-variants.png` | 4×2 | 无装备、根、茎、花、根+茎、根+花、茎+花、根+茎+花 |
| `inventory-b-vfx-root.png` | 4×2 | 铁甲根嫁接动画，第 1—8 帧 |
| `inventory-b-vfx-vine.png` | 4×2 | 藤蔓触须嫁接动画，第 1—8 帧 |
| `inventory-b-vfx-flower.png` | 4×2 | 毒菌伞嫁接动画，第 1—8 帧 |
| `inventory-b-effect-icons-atlas.png` | 3×2 | 防御、冲刺格挡、藤鞭、感知/拾取、毒云、收集/组合 |
| `inventory-b-category-badges-atlas.png` | 4×2 | 三部位瓶装标识及辅助图标；新获得、已装备、锁定、撤销 |
| `inventory-b-focus-overlays.png` | 3×1 | 根、茎、花的焦点/选中覆盖层 |
| `inventory-b-connectors-atlas.png` | 4×3 | 未点亮/点亮的直线、转角、T 形和端点连接变化 |
| `inventory-b-panels-atlas.png` | 2×2 | 标题牌、分区标题、详情面板、提示面板 |

## 背包展示逻辑建议

- “拾取”只改变物品格、`NEW` 标记和对应通知，不直接改变角色外形。
- “装备”后点亮对应六边形槽位、汁液连接线和角色部位，并切换到 8 种组合之一。
- 铁甲根对应 `伤害降低 25%`、`冲刺可挡投射物`；藤蔓触须对应藤鞭能力；毒菌伞对应持续 3 秒的毒云。
- 空槽、已获得、当前选中、已装备、锁定必须保持不同视觉状态，避免只靠文字区分。

`Assets/Game/Editor/InventoryArtImporter.cs` 会把本目录 PNG 自动设为 Sprite、关闭压缩与 Mipmap，并按上表网格切片。
