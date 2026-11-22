# 要不要进货（ShouldIRestock）

柜台电话下沿的一张便签。点开后按游戏标签做**物品分类统计**：每个品类一行，带颜色和件数，相当于一份品类记录、标签记录。悬停那一行，对应库存会描边；点一下就锁定，再去看别的标签也不会换。行末标出赃物和违禁品数量，顶部还可以单独看全部赃物、全部违禁品。用来看店里还缺哪一类，决定要不要进货。

Inventory Category Stats. A sticky note under the counter phone. It tallies stock by the game's item tags. Hover a row to outline that category; click to lock the highlight. Stolen goods get an amber corner, contraband a red one.

## 安装

编译会把文件拷到游戏目录的 `Mods\should_i_restock\`。游戏里打开「模组」，启用「要不要进货」，再重启一次。

```
dotnet build ShouldIRestock.sln -c Release
```

游戏路径写在 `Directory.Build.props` 的 `GameDir`。只编译、不拷进游戏时加上 `-p:DeployToMods=false`。

## 用法

1. 读档进店。便签贴在电话下沿。
2. 点便签，打开黄色便签纸。第一页开头是「全部赃物」「全部违禁品」，后面按标签列出 `[品类] (件数)`，有赃物或违禁品时行末写 `赃 N`、`违 M`。
3. 鼠标停在一行上，店里这个标签的货（背包、前后柜台、暗格、展示柜、仓库）会按品类颜色描边。赃物右上角是琥珀点，违禁品是红点。
4. 点一行锁定。锁定之后，悬停别的行不再改高亮。再点同一行解除。
5. 品类多了用纸下方的翻页。关掉纸或睡觉会取消悬停，锁定还在。
6. 模组齿轮里可以改描边粗细、刷新间隔，以及要不要跳过上锁容器和杂物堆。

统计的是你自己的货。顾客放在柜台上、还没买下来的东西不算。

## 调试

进店后玩家日志里会有一行探针，写出电话的父节点、尺寸，以及前几件货命中了哪些标签。日志在：

`%USERPROFILE%\AppData\LocalLow\Questing Goose Studio\Probably Stolen\Player.log`

要在游戏里看，在 `Probably Stolen.exe` 旁边放一个空的 `dev.txt`，按 F8。不要把 `dev.txt` 和模组一起发出去。
