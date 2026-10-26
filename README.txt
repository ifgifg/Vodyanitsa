沃雅妮莎 Vodyanitsa 1.0.0
完整公开说明见 README.md；素材接口见 ASSET-CONTRACT.txt。
源码：https://github.com/ifgifg/Vodyanitsa

将发布的 Vodyanitsa.tmod 放入 tModLoader 的 Mods 文件夹并启用。
游戏模组界面可以打开该文件夹。无需时装或指令。
普通水中按绑定的方向键自由游泳，跳跃键上浮；没有额外资源消耗，
并可保持水下呼吸。熔岩、蜂蜜、微光不触发这些能力。
装备属性、存档外貌、原版碰撞保留；尾巴不改变碰撞盒。
关闭配置的 Enabled 或停用模组可恢复原外貌和原版运动。

Vodyanitsa 与 OdetteCharacter 同时启用，且 Vodyanitsa.Enabled=true
时，沃雅妮莎优先，奥黛塔主动让位。使用奥黛塔时停用 Vodyanitsa
或关闭其 Enabled 配置；无需删除文件。旧 OdetteVanity 可继续保留，
避免已有时装物品变成未加载物品。模组不会自行修改其他模组的设置。

构建：保持源码目录名为 Vodyanitsa，PowerShell 执行 .\build.ps1。
.\build.ps1 -CompileOnly 仅编译；-TMLPath/-Dotnet 可指定本机路径。
需要 .NET 8 SDK 和本机 tModLoader，输出在相邻 VodyanitsaBuild\release。
构建使用独立 runtime/save，不安装、不启用、不读取真实人物或世界。
已验证 Windows / tModLoader 2026.8.3.0 / Terraria 1.4.4.9，其他平台未验证。

二维图集与前臂旋转不等于完整骨骼动画或原角色全部技能。
其他模组的自定义人物外观层可能被覆盖，不能保证所有第三方特效兼容。
已检查原生渲染、实际水中运动/呼吸/碰撞、服务器加载及整合包启动；
未完成真实双客户端联机实战测试，服务器能加载不等于联机全面验证。

原创贡献采用 LICENSE 中的 MIT 许可。角色和其他第三方既有知识产权
仍归原权利人，详见 THIRD_PARTY_NOTICES.md。图集使用 AI 辅助生成。
公开文件清单为 PUBLIC-SOURCE-MANIFEST.txt，不含研究参考图、私人日志、
配置、存档或游戏库。二进制发布包与源码目录分别管理。
