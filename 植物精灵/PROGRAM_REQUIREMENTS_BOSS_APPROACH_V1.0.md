# 植物精灵程序需求规格：腐化古树巢穴前引导 V1.0

来源：`deepseek_markdown_20260905_1ce093.md` 第 5.2 节 Boss 房配置。本规格补充 BossArena 开战前的回血、迷雾和叙事提示，不改变腐化古树的 150 HP 与果实 -> 地刺 -> 树人攻击循环。

## 1. 玩家流程

`Level01` 的四波杂兵全部清除后，开放通往 `BossArena` 的传送门。玩家进入 BossArena 后，依序经过回血祭坛、白色迷雾门和腐化古树巢穴；白雾触发提示后，Boss 保持在巢穴内等待战斗。

## 2. 场景布置

| 元素 | 位置关系 | 视觉和交互 |
|---|---|---|
| 回血祭坛 | BossArena 出生点前方，白雾前 | 青绿色植物祭坛、微弱上升粒子；玩家触碰后回满生命 |
| 白色迷雾 | 祭坛与 Boss 巢穴之间，横跨主通道 | 半透明、缓慢流动的冷白雾墙；使用触发器，不阻挡玩家和投射物 |
| 腐化古树巢穴 | 白雾之后 | 腐败根须、紫色腐化痕迹、深色树影，Boss 位于巢穴中心 |

回血祭坛每次进入 BossArena 可使用一次。使用后保留已激活外观，不重复播放回血或回满生命事件。白雾在本次 BossArena 进程中只触发一次。

## 3. 白雾叙事提示

玩家首次进入白雾触发器时，显示以下精确文案：

`前方就是腐化古树的巢穴.....`

显示规则：

1. 文字在屏幕下方居中显示，位于 HUD 之上，不遮挡生命值和嫁接栏。
2. 文字在 0.35 秒内淡入，停留 2.4 秒，随后在 0.45 秒内淡出。
3. 每个字底部在淡入完成后生成 1 至 2 条紫色液滴；液滴先拉长 0.18 至 0.3 秒，再向下坠落并在 0.35 至 0.6 秒内溶解。
4. 液滴颜色从暗紫 `#6B287F` 过渡到亮紫 `#B35AE8`，透明度递减；不得遮住输入、生命值或嫁接 UI。
5. 效果播放期间不暂停游戏、不锁定移动、不触发 Boss 攻击状态变化；再次进入雾区不重复播放。
6. 若玩家死亡、重开或返回菜单，提示状态和残留液滴必须清理。

## 4. 程序接口与状态

| 模块 | 职责 |
|---|---|
| `HealingShrine` | 保留现有触碰回满生命逻辑，新增一次性激活外观状态 |
| `BossFogTrigger` | 监听玩家首次进入，通知 UI，维护 `triggered` 状态 |
| `BossApproachPrompt` | 管理文案、淡入淡出与字下液滴对象池；场景卸载时回收所有粒子 |
| `LevelFlow` | 在 `BossArena` 按出生点 -> 祭坛 -> 白雾 -> Boss 的顺序生成或定位上述对象 |

场景对象建议命名：`HealingShrine`、`BossFogWall`、`BossApproachPrompt`、`CorruptedAncientNest`。白雾碰撞体使用 Player 层触发掩码，宽度覆盖通道，高度覆盖玩家跳跃高度。

## 5. 美术提示词

### 场景资产

```text
2D side-scrolling action game boss approach, the threshold to the Corrupted Ancient's nest in a dark botanical ruin. A small living healing shrine stands before the entrance: pale stone pedestal wrapped in healthy teal-green vines, a warm mint glow in its heart, tiny rising spores, clearly readable as a safe healing point. Behind it, a tall wall of soft white mist stretches across the passage, semi-transparent layered fog ribbons flowing sideways and upward, edges feathered, no hard rectangular border. Beyond the mist is the silhouette of a colossal rotten ancient tree nest: black tangled roots, warped bark, subtle violet corruption veins, deep shadowed cavern space. Hand-painted 2D game environment, readable platformer collision silhouettes, foreground / midground / background separation, restrained teal, white, charcoal and violet palette, dramatic but playable, no characters, no UI, no text, no logos, transparent-background props where appropriate.
```

### UI 液滴特效资产

```text
2D game UI VFX sprite sheet, corrupt purple liquid dripping from the bottom edge of title lettering. Frames show: a small dark-purple bead forms, stretches into a thin glossy strand, detaches into a falling droplet, splashes softly and dissolves into violet mist. High contrast against a transparent background, dark purple #6B287F to luminous violet #B35AE8, subtle wet shine, clean readable silhouette, no letters, no words, no frame border, no logo, no background.
```

## 6. 验收标准

1. 清完 `Level01` 四波小怪后，只开放前往 BossArena 的传送门，不在第一场景生成 Boss。
2. 进入 BossArena 后，回血祭坛位于白雾之前；首次触碰祭坛后玩家生命恢复至最大值。
3. 玩家可直接穿过白雾，且首次穿过时只显示一次指定文案和紫色下滴效果。
4. 文案和液滴不暂停战斗、不锁定输入，也不遮挡主要 HUD。
5. BossArena 卸载、死亡重开或返回菜单后，白雾提示和所有液滴对象均被清理。
