# Terrain
目前仓库将地形算法制作成了一个Unity Package，因此与以往开发方式有所不同；首先你需要创建一个`Unity 2022.3.50f1`的URP空项目

# For Use Only
如果你仅仅是使用package，而不进行开发，点击：Window/PackageManager打开UPM包管理器，点击左上角加号，选择从git链接安装

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1758449897940-064fcaa7-4303-418d-b2bb-f2dd36828483.png" width="976" title="" crop="0,0,1,1" id="uc10b1d99" class="ne-image">

输入项目的git链接下载安装即可

```plain
https://github.com/ChenJiaming5613/GaiaTerrain.git
```

# For Development
如果你想进行开发工作就不能使用上述git安装方式；最简单的开发方式是你需要进入到Unity项目工程的`Packages`目录，然后正常的克隆仓库，Unity会自动识别`Packages`目录下的package并导入到项目中

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1758450201088-2b8e5552-ee64-4498-9944-72cfe2089a8d.png" width="573.8666666666667" title="" crop="0,0,1,1" id="ub10e4b72" class="ne-image">

# 安装Package
新建一个URP项目，安装下面几个package

+ [[AssetStore] unity-terrain-urp-demo-scene-213197](https://assetstore.unity.com/packages/3d/environments/unity-terrain-urp-demo-scene-213197)
+ [[AssetStore] starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526](https://assetstore.unity.com/packages/essentials/starter-assets-thirdperson-updates-in-new-charactercontroller-pa-196526) （默认安装后会重启Unity）

安装Gaia Terrain的package

# 修改Demo资产
打开`DemoImportSettingWindow`面板

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759771526457-1b29ef97-0e13-49f7-aebb-23975bcd618e.png" width="527.2" title="" crop="0,0,1,1" id="u71d5f750" class="ne-image">

填入Demo资产路径，点击Apply

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759771603181-f20aba65-c63e-455e-ac13-3822f8836a78.png" width="936.8" title="" crop="0,0,1,1" id="uacac83b3" class="ne-image">

# TerrainAsset制作
打开`GaiaTerrainAssetMaker`面板

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759771565004-5fa7102a-1227-4015-a95d-7e148ffc720c.png" width="531.2" title="" crop="0,0,1,1" id="u1218586d" class="ne-image">

全选所有的TerrainData

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759746466982-547bda65-1d8f-4a13-b974-96b4e89e16b1.png" width="600.8" title="" crop="0,0,1,1" id="ua444314f" class="ne-image">

依次点击

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759746493774-0679966d-9eeb-4d56-b6f0-109f17d5227e.png" width="531.6000366210938" title="" crop="0,0,1,1" id="u9eeead41" class="ne-image">

点击制作资产

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759746528999-7bc78034-9ea5-4d83-b258-cfa432b5da39.png" width="557" title="" crop="0,0,1,1" id="u609acd15" class="ne-image">

# 搭建场景
新建一个空的场景，将Unity的Terrain Prefab拖入场景

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759773700644-33333560-5e4c-4142-806c-a084c1a49d61.png" width="460" title="" crop="0,0,1,1" id="vzDMA" class="ne-image">

再将Third Person中的角色Prefab拖入场景，删除场景原有的Main Camera

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759773678511-d8415234-35da-4d2a-9e4c-3e2a2a3761ce.png" width="373.6" title="" crop="0,0,1,1" id="WUCAj" class="ne-image">

修改原生Terrain的中心为世界原点

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1759773653627-fc71dc1a-0ada-40d1-b1b9-0d9cf88f6b8d.png" width="535.6000366210938" title="" crop="0,0,1,1" id="dxYre" class="ne-image">

添加GaiaTerrain

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1760081844448-992a0f59-01a3-4511-a80d-2c258836adff.png" width="457.81817626953125" title="" crop="0,0,1,1" id="u52758180" class="ne-image">

添加TerrainSwitcher

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1760081815019-686d74d8-58eb-4c81-bb13-318137cd9b0d.png" width="521.45458984375" title="" crop="0,0,1,1" id="u0b050e79" class="ne-image">

URP添加GaiaTerrainRendererFeature

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1760081871257-e813f531-01c0-4d62-9526-c4f59ab87de0.png" width="496.81817626953125" title="" crop="0,0,1,1" id="uaeb75fea" class="ne-image">

最终的场景组织

<img src="https://cdn.nlark.com/yuque/0/2025/png/29081253/1760082007391-8ac78a2b-dc6b-427f-af66-7ef461e1b48c.png" width="449.45454545454544" title="" crop="0,0,1,1" id="u88bb1ce7" class="ne-image">

# 运行测试
+ 按键盘`C`切换地形算法


