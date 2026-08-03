# 从零做出执法记录仪视角：Bodycam 系统源码教程

> 对应文件：
>
> - `Assets/Dead Earth/Shaders/BodycamPost.shader`
> - `Assets/Dead Earth/Scripts/FPS Controller/BodycamPostEffect.cs`
> - `Assets/Dead Earth/Scripts/FPS Controller/BodycamSway.cs`
>
> 适用项目：Unity 2022、Built-in Render Pipeline（内置渲染管线）。

这套效果不是“一个 Shader”就能完成的。它由两条链路组成：

1. **运动链路**：让相机像固定在胸口，而不是像稳定器固定在头上。
2. **画面链路**：把相机渲出的整张画面交给 Shader，加入镜头畸变、暗角、色差、噪点和扫描线。

先建立一个总心智模型：

```text
FPS Controller Rig（玩家本体，处理输入、移动、左右转身）
└─ BodycamSway（本教程的运动惯性：位置 + 倾斜）
   └─ CameraRecoil（项目原有的后坐力）
      └─ FPS Camera（上下看、渲染场景、后处理）
         └─ BodycamPostEffect（把画面送入 BodycamPost.shader）
```

相机每帧的结果可以理解为：

```text
玩家移动和转身
    → BodycamSway 晚一点跟随（胸前惯性）
    → CameraRecoil 叠加开枪后坐力
    → FPS Camera 负责上下看和拍摄场景
    → BodycamPostEffect 把拍到的画面交给 Shader
    → 最终显示在屏幕上
```

---

## 1. 先分清三个文件分别负责什么

| 文件 | 属于哪一层 | 负责的事 | 不负责的事 |
| --- | --- | --- | --- |
| `BodycamSway.cs` | C# 运动层 | 根据移动速度与转身速度，让相机父物体轻微位移、侧倾 | 改变画面颜色、加噪点 |
| `BodycamPostEffect.cs` | C# 渲染桥梁 | 取得相机渲染结果，建立材质，传递参数，调用 Shader | 计算每一个像素的颜色 |
| `BodycamPost.shader` | GPU 像素层 | 对屏幕上每一个像素做畸变、暗角、色差、噪点等计算 | 读取玩家键盘输入、移动相机 |

**必须掌握：** C# 决定“何时渲染、参数是多少”；Shader 决定“一个像素最后长什么样”。

一个常见误区是：在 `BodycamSway` 里做暗角，或者在 Shader 里读取玩家速度。虽然理论上能绕着做到，但职责会混乱、很难维护。现在这个拆分是比较健康的结构。

---

## 2. 从零挂载：先让结构正确，再写效果

### 2.1 相机层级为什么要这样放

`FPSController` 会控制 **FPS Camera 自己** 的上下看（Pitch）；`BodycamSway` 则控制 **相机的父物体** 的胸口惯性。

这样两者不会抢同一个 `transform.localRotation`：

```text
BodycamSway.localRotation   = 左右横移/转身造成的侧倾
FPS Camera.localRotation    = 鼠标上下看
```

世界里的最终旋转会自然叠加。相机既能上下看，也会因为奔跑和甩鼠标产生胸前设备的微晃。

如果把两个脚本都挂在 `FPS Camera` 上，它们都会每帧写相机的局部旋转，后执行的那个会覆盖先执行的那个，表现就是“有一个效果失效”或“抖动”。

### 2.2 实际挂载步骤

1. 在玩家根物体与 `FPS Camera` 之间创建空物体，命名为 `BodycamSway`。
2. 将 `FPS Camera`（以及已有的 `CameraRecoil`）放到它下面。
3. 给 `BodycamSway` 挂 `BodycamSway.cs`。
4. 给真正带有 `Camera` 组件的 `FPS Camera` 挂 `BodycamPostEffect.cs`。
5. 将 `BodycamPost.shader` 拖入 `BodycamPostEffect` 的 **Shader** 字段。
6. 从全部参数为 `0` 开始，确认画面没有变化，再逐项加效果。

### 2.3 本项目的更新顺序为什么合理

项目中的 `FPSController.Update()` 会先处理鼠标、移动、蹲伏和头部起伏；`BodycamSway` 使用 `LateUpdate()`。

`LateUpdate` 的意思是“这一帧大部分普通 `Update` 都跑完之后再跑”。因此 `BodycamSway` 读到的是本帧最新的 `CharacterController.velocity` 和玩家朝向，而不是上一帧的旧数据。

这是相机跟随、镜头惯性、骨骼跟随类效果常用的做法。

---

## 3. `BodycamSway.cs`：把速度翻译成胸口惯性

### 3.1 字段与 Inspector 参数

```csharp
[SerializeField] private FPSController _controller;
```

`[SerializeField]` 的作用是：字段虽然是 `private`，仍然显示在 Unity Inspector，便于调参；`private` 则保证其他脚本不能随便改它。

`_controller` 是数据来源。它提供：

- `characterController.velocity`：玩家实际速度；
- `transform.eulerAngles.y`：玩家左右朝向；
- 通过父物体关系自动找到自身所在的玩家。

下面的参数全部是“每单位速度，产生多少效果”的系数：

| 参数 | 含义 | 太大时会怎样 |
| --- | --- | --- |
| `_strafeRollPerSpeed` | 横移每 `1 m/s` 产生多少侧倾角 | 像坐船，横移很夸张 |
| `_movePitchPerSpeed` | 前进每 `1 m/s` 产生多少俯仰角 | 跑步时镜头点头过猛 |
| `_strafeOffsetPerSpeed` | 横移每 `1 m/s` 产生多少横向位移 | 相机像漂浮在身体旁边 |
| `_forwardOffsetPerSpeed` | 前进每 `1 m/s` 产生多少前后位移 | 奔跑时进退感过头 |
| `_turnRollPerDegreePerSecond` | 转身速度转为侧倾的比例 | 甩鼠标时猛翻相机 |
| `_maxTurnRoll` | 转身侧倾的上限 | 太低不明显；太高易眩晕 |
| `_smoothSpeed` | 跟随目标的速度 | 太低拖沓，太高像没惯性 |

### 3.2 `Awake`：找引用，并消除第一帧跳变

```csharp
private void Awake()
{
    if (_controller == null)
        _controller = GetComponentInParent<FPSController>();

    if (_controller != null)
        _previousYaw = _controller.transform.eulerAngles.y;
}
```

这里包含两个很实用的编程习惯：

1. **Inspector 可手动指定，也可自动兜底。** 手动拖引用最明确；忘了拖时仍会从父物体查找。
2. **记录初始状态。** `yawSpeed` 要用“这一帧角度 - 上一帧角度”。如果 `_previousYaw` 没初始化，第一帧可能从 `0°` 突然算到玩家的真实角度，产生不合理的瞬间侧倾。

### 3.3 为什么不能直接使用世界速度

```csharp
Vector3 worldVelocity = _controller.characterController.velocity;
worldVelocity.y = 0f;

Vector3 localVelocity =
    _controller.transform.InverseTransformDirection(worldVelocity);
```

`velocity` 在世界坐标中。例如玩家面朝北方时，向前走可能是世界 `Z+`；转向东边后，向前走可能是世界 `X+`。

但我们真正关心的是：**相对于玩家自己，他是在向左、向右、向前还是向后？**

`InverseTransformDirection` 把世界方向转回玩家局部坐标：

```text
localVelocity.x < 0：向左横移
localVelocity.x > 0：向右横移
localVelocity.z > 0：向前移动
localVelocity.z < 0：向后移动
```

`worldVelocity.y = 0f` 是刻意忽略上下速度。否则跳跃、下落会被误判成“镜头应该前后晃”，这不符合本教程的胸前设备风格。

### 3.4 计算转身速度：为什么要用 `Mathf.DeltaAngle`

```csharp
float yawSpeed = Mathf.DeltaAngle(_previousYaw, currentYaw)
                 / Mathf.Max(Time.deltaTime, 0.0001f);
```

角度会循环：`359°` 后面是 `0°`。普通减法会得到 `0 - 359 = -359°`，明明只是轻微转了一点，却被认为是猛烈转身。

`Mathf.DeltaAngle(359, 0)` 会给出正确的最短差值 `+1°`。再除以 `deltaTime`，单位就变成“度/秒”，也就是转身速度。

`Mathf.Max(Time.deltaTime, 0.0001f)` 是防御式编程：避免极端情况下除以 `0`。

### 3.5 由速度得到目标姿势

```csharp
float moveRoll = -localVelocity.x * _strafeRollPerSpeed;
float turnRoll = Mathf.Clamp(
    -yawSpeed * _turnRollPerDegreePerSecond,
    -_maxTurnRoll,
    _maxTurnRoll
);
```

负号不是数学错误，而是美术选择：向右横移或向右甩身时，胸前设备略微向左滞后，会让人感觉它有重量。

`Clamp` 是视觉效果的“保险丝”。鼠标可以在一帧移动非常远，若没有上限，镜头会突然倾斜几十度。

最后组合为：

```csharp
Vector3 targetRotation = new Vector3(
    localVelocity.z * _movePitchPerSpeed,
    0f,
    moveRoll + turnRoll
);

Vector3 targetPosition = new Vector3(
    -localVelocity.x * _strafeOffsetPerSpeed,
    0f,
    -localVelocity.z * _forwardOffsetPerSpeed
);
```

这段代码最关键的设计是：`Y = 0`。它不处理左右朝向，因为左右转身仍属于玩家本体；它只添加相机父物体的局部偏移和侧倾。

### 3.6 不用固定 `Lerp`，而用指数平滑

```csharp
float smoothing = 1f - Mathf.Exp(-_smoothSpeed * Time.deltaTime);
```

很多初学者会写：

```csharp
transform.localPosition = Vector3.Lerp(
    transform.localPosition, targetPosition, 0.1f);
```

这会让不同帧率有不同手感：60 FPS 每秒插值 60 次，30 FPS 每秒只插值 30 次。

当前写法把 `deltaTime` 纳入计算，`_smoothSpeed` 更接近“每秒追赶的速度”。因此在 30、60、120 FPS 下，惯性更稳定。

```csharp
transform.localRotation = Quaternion.Slerp(
    transform.localRotation,
    Quaternion.Euler(targetRotation),
    smoothing
);

transform.localPosition = Vector3.Lerp(
    transform.localPosition,
    targetPosition,
    smoothing
);
```

旋转使用 `Quaternion.Slerp`，避免直接插值欧拉角可能出现的角度绕行；位置使用普通 `Vector3.Lerp` 即可。

### 3.7 这个版本的一个前提：中立姿势必须是零

当前脚本空闲时会把：

```text
localPosition 追向 (0, 0, 0)
localRotation 追向 (0, 0, 0)
```

所以它假定 `BodycamSway` 这个空物体的中立局部位置是零、局部旋转是单位旋转。当前层级通常正好符合这个假定。

若以后你把 `BodycamSway` 人为旋转了 `5°` 或平移了 `0.1 m`，脚本会在游戏中慢慢把它拉回零，导致摆放偏移丢失。更通用的版本应在 `Awake` 缓存中立姿势：

```csharp
private Vector3 _baseLocalPosition;
private Quaternion _baseLocalRotation;

private void Awake()
{
    _baseLocalPosition = transform.localPosition;
    _baseLocalRotation = transform.localRotation;
    // 原有自动查找 Controller 的代码……
}
```

然后把目标改为“基础姿势 + 偏移”。这是下一步重构时很值得练习的改进。

---

## 4. `BodycamPostEffect.cs`：C# 如何把相机画面交给 Shader

### 4.1 两个特性

```csharp
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class BodycamPostEffect : MonoBehaviour
```

- `RequireComponent(typeof(Camera))`：这个组件只能依附在有 `Camera` 的物体上。添加时 Unity 会自动补 Camera，避免挂错对象。
- `ExecuteAlways`：编辑模式也会执行。因此不用每次点击 Play 才能预览噪点、暗角等画面参数。

> 注意：编辑模式实时预览很方便，但它也意味着改 Inspector 后场景会变脏（标题出现 `*`）。确认满意后再保存场景。

### 4.2 Inspector 参数为什么使用 `[Range]`

```csharp
[Range(0f, 10f)]
[SerializeField] private float _vignette = 0.38f;
```

`Range` 会把 Inspector 中的输入变成滑条，并限制安全区间。对于视觉参数很有价值：它既方便美术调试，也防止输入离谱的数值。

这些 C# 字段通过下面的代码传给 Shader：

```csharp
_material.SetFloat("_Vignette", _vignette);
```

字符串 `"_Vignette"` 必须与 Shader 中的变量名完全一致。少一个下划线、拼写不同，Shader 就收不到值。

### 4.3 `OnRenderImage` 是什么

```csharp
private void OnRenderImage(RenderTexture source, RenderTexture destination)
```

在 Built-in Render Pipeline 中，这个 Unity 回调会在相机渲完场景后调用：

```text
source      = 相机刚刚渲出来的完整画面
destination = 最终要写入的画面目标
```

最简单的后处理就是不做任何处理，直接复制：

```csharp
Graphics.Blit(source, destination);
```

这也是脚本的失败兜底：

```csharp
if (_shader == null || !_shader.isSupported)
{
    Graphics.Blit(source, destination);
    return;
}
```

因此即使 Inspector 忘记指定 Shader，玩家至少还能正常看见游戏，而不是黑屏。

> **重要：** `OnRenderImage` 是 Built-in 管线的旧式图像效果接口。这个项目可以使用它；若项目升级为 URP/HDRP，应改用 Renderer Feature / Full Screen Pass，而不是照抄此脚本。

### 4.4 为什么要延迟创建 Material

```csharp
if (_material == null)
{
    _material = new Material(_shader)
    {
        hideFlags = HideFlags.DontSave
    };
}
```

Shader 是一份 GPU 程序；Material 是它的一份“可填写参数的实例”。每帧 `new Material` 会不断分配内存、产生卡顿，所以只在第一次渲染时创建一次，然后一直复用。

`HideFlags.DontSave` 表示这是运行时临时材质，不把它偷偷写进场景或资源文件。

### 4.5 本项目为什么使用中间 RenderTexture

项目的相机使用 HDR Deferred 渲染目标，并且同时有其他旧式图像效果。直接把 `source` 交给第二个效果时，曾出现过画面发灰的问题。

所以当前流程是：

```text
source（相机 HDR/Deferred 输出）
    ↓ Graphics.Blit
intermediate（临时、单采样的普通纹理）
    ↓ Bodycam Shader
destination（最终屏幕画面）
```

对应代码：

```csharp
RenderTextureDescriptor descriptor = source.descriptor;
descriptor.depthBufferBits = 0;
descriptor.msaaSamples = 1;
descriptor.useMipMap = false;
descriptor.autoGenerateMips = false;

RenderTexture intermediate = RenderTexture.GetTemporary(descriptor);
try
{
    Graphics.Blit(source, intermediate);
    _material.SetTexture("_MainTex", intermediate);
    Graphics.Blit(intermediate, destination, _material);
}
finally
{
    RenderTexture.ReleaseTemporary(intermediate);
}
```

逐项理解：

- `source.descriptor`：复制源画面的分辨率、格式等基本描述；
- `depthBufferBits = 0`：后处理只读颜色，不需要深度缓冲；
- `msaaSamples = 1`：Shader 读取单采样纹理，避免多重采样目标兼容问题；
- 不创建 Mipmap：画面始终全屏读取，Mipmap 没有必要；
- `GetTemporary`：向 Unity 的临时纹理池借一张纹理；
- `finally`：无论中间过程是否报错，都归还纹理，避免显存泄漏。

**必须掌握：** `GetTemporary` 与 `ReleaseTemporary` 必须成对出现。只要借了临时 RenderTexture，就要确保归还。

### 4.6 为什么在 `OnDisable` 销毁 Material

```csharp
private void OnDisable()
{
    if (_material == null) return;

    if (Application.isPlaying)
        Destroy(_material);
    else
        DestroyImmediate(_material);
}
```

运行时用 `Destroy`，让 Unity 在安全时机处理对象；编辑模式没有正常的帧结束销毁流程，所以使用 `DestroyImmediate`。这能避免你在 Inspector 反复开关组件时遗留多个隐藏材质。

---

## 5. `BodycamPost.shader`：逐步理解每个像素怎样被处理

### 5.1 Shader 的外壳：`Properties` 与运行时变量

```shader
Properties
{
    _MainTex ("Source", 2D) = "white" {}
    _LensDistortion ("Lens Distortion", Range(0, 0.2)) = 0
    _Vignette ("Vignette", Range(0, 10)) = 0
    ...
}
```

`Properties` 是给 Unity 和材质 Inspector 看的声明；下面的：

```shader
sampler2D _MainTex;
float _Vignette;
```

才是 GPU 程序里真正读取它们的变量。

`_MainTex` 的特别之处是：它不是某张固定图片，而是 `BodycamPostEffect` 传入的“本帧相机画面”。

> 当前 Shader 与 C# 都允许 `_Vignette` 到 `10`，但这个效果的视觉安全范围仍然是 `0–1`。超过 `1` 会把角落压得接近全黑，容易误判为渲染故障；日常调参请保持在 `0.15–0.35`。如果你希望滑条本身阻止过大数值，可将两处范围统一收紧为 `Range(0, 1)`。

### 5.2 全屏后处理的渲染状态

```shader
Cull Off ZWrite Off ZTest Always
```

后处理不是渲染 3D 模型，而是渲染覆盖整个屏幕的一张矩形：

- `Cull Off`：矩形正反面都可绘制；
- `ZWrite Off`：不要把这张全屏图写入深度；
- `ZTest Always`：不管场景深度如何，都要覆盖输出。

### 5.3 顶点阶段：只负责把全屏矩形送到屏幕

```shader
struct appdata
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2f
{
    float2 uv : TEXCOORD0;
    float4 vertex : SV_POSITION;
};
```

`appdata` 是输入顶点的数据，`v2f` 是顶点阶段交给像素阶段的数据。这里真正重要的是 `uv`：它描述屏幕上的相对位置，左下接近 `(0, 0)`，右上接近 `(1, 1)`。

```shader
v2f vert(appdata v)
{
    v2f o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.uv = v.uv;
    return o;
}
```

这部分没有制造视觉效果，它只是让每个屏幕像素都能知道“我应该从源画面的哪个 UV 位置取颜色”。真正的魔法在 `frag` 中发生。

### 5.4 将 UV 平移到屏幕中心

```shader
float2 centeredUv = i.uv - 0.5;
float radius = length(centeredUv);
float radiusSquared = dot(centeredUv, centeredUv);
```

普通 UV 的中心是 `(0.5, 0.5)`。减去 `0.5` 后，屏幕中心变成 `(0, 0)`，更容易计算到中心的距离。

```text
中心：radius ≈ 0
边缘：radius 变大
角落：radius 最大
```

`radiusSquared` 等于 `radius * radius`。它让效果在中心更弱、在边缘增长更快，而且不必额外开平方。

### 5.5 镜头畸变：改变“去哪里取样”

```shader
float2 uv = 0.5 + centeredUv *
    (1.0 + _LensDistortion * radiusSquared * 4.0);
```

注意：后处理中的鱼眼通常不是把颜色涂弯，而是**改变采样坐标**。边缘像素会从更靠外的位置取颜色，形成轻微广角/桶形感。

`_LensDistortion = 0` 时，乘数为 `1`，采样坐标完全不变，因此是可靠的“关闭效果”状态。

之后的：

```shader
tex2D(_MainTex, saturate(uv))
```

中的 `saturate` 会把 UV 限制到 `0–1`。没有它，鱼眼向外拉时会访问纹理边界之外，可能采样到重复或边缘色。

### 5.6 色差：让红绿蓝从略微不同的位置取样

```shader
float2 direction = radius > 0.0001
    ? centeredUv / radius
    : float2(0, 0);
float2 colourOffset =
    direction * _ChromaticAberration * radiusSquared;
```

`direction` 是从屏幕中心指向当前像素的单位方向。除以 `radius` 前先判断它是否接近零，是为了避免屏幕中心出现“除以零”。

```shader
colour.r = tex2D(_MainTex, saturate(uv + colourOffset)).r;
colour.b = tex2D(_MainTex, saturate(uv - colourOffset)).b;
```

红、绿、蓝在不同位置取样，边缘高反差处就会出现非常轻的彩边。使用 `radiusSquared` 后，中心几乎没有色差，越靠边越明显，更像真实镜头。

代码在 `_ChromaticAberration` 为零时跳过额外两次采样。这既省性能，也确保关闭色差时不会意外改色。

### 5.7 暗角：为什么 `smoothstep` 比直接乘距离好

```shader
float edge = smoothstep(0.38, 0.74, radius);
colour *= 1.0 - edge * _Vignette;
```

`smoothstep(a, b, x)` 的输出：

```text
x ≤ a：0
x ≥ b：1
中间：平滑地从 0 过渡到 1
```

所以这里：

- 中心区域 `edge = 0`，颜色完全不变；
- 从半径 `0.38` 开始平滑变暗；
- 接近角落时 `edge` 接近 `1`；
- `_Vignette = 0.25` 时，最边缘约变为原亮度的 `75%`。

这种写法只压低 RGB，不会向画面额外叠一层灰色。此前画面“灰”并不是暗角本身的正常结果，而是后处理源纹理兼容/Shader 重新导入问题。

### 5.8 传感器噪点与移动扫描线

```shader
float noise = frac(sin(dot(
    i.uv * _ScreenParams.xy + _Time.yy,
    float2(12.9898, 78.233))) * 43758.5453);
```

这是一种没有噪点贴图时常见的伪随机写法：UV 和屏幕尺寸决定空间位置，`_Time` 让它每帧变化，`frac` 把数值截到 `0–1`。

```shader
colour += (noise - 0.5) * _NoiseIntensity;
```

`noise - 0.5` 把噪点中心移到零：有些像素稍亮，有些稍暗，平均不会持续把整张图越加越亮。

原有的传感器扫描线使用纵向正弦波，并随时间向下滚动：

```shader
float movingScanline =
    sin((i.uv.y * _ScreenParams.y + _Time.y * 8.0)
    * 3.14159265) * 0.5 + 0.5;

colour *= 1.0 - movingScanline * _ScanlineIntensity;
```

它适合模拟传感器/传输过程中的轻微干扰，`Scanline Intensity` 应保持很低。它只会变暗，不会叠灰；强噪点会吞掉场景细节，而不是增加真实感。

### 5.9 CRT 横向扫描线：可匀速滚动的老显示器暗线

CRT 与上面的移动传感器扫描线不是同一个概念。老式 CRT 显示器会逐行扫描，画面上会留下横向暗线；本实现还可以将整组暗线以恒定速度上下滚动：

```shader
float crtWave =
    sin((i.uv.y * _CrtScanlineDensity +
         _Time.y * _CrtScanlineScrollSpeed) * 6.2831853)
    * 0.5 + 0.5;
float crtDarkLine = 1.0 - smoothstep(
    0.0,
    _CrtScanlineThickness,
    crtWave
);
colour *= 1.0 - crtDarkLine * _CrtScanlineIntensity;
```

它的思路是：

1. `_CrtScanlineDensity` 决定屏幕从上到下重复多少次波形，也就是有多少条横线；
2. `crtWave` 每次到达低谷时代表一条扫描线的位置；
3. `smoothstep` 将低谷变成有柔和边缘的暗带，而不是锯齿状的一像素硬线；
4. `_CrtScanlineIntensity` 决定暗带最多能压暗多少亮度。
5. `_CrtScanlineScrollSpeed` 为整组横线添加相同的时间偏移，因此它们会匀速平移，不会各自闪烁。

新增的 Inspector 参数：

| 参数 | 建议范围 | 视觉意义 |
| --- | ---: | --- |
| CRT Scanline Intensity | `0.08–0.18` | 暗线有多深；过高会脏、会压暗细节 |
| CRT Scanline Density | `180–320` | 横线数量；值大，线更密、更细 |
| CRT Scanline Thickness | `0.08–0.16` | 每条暗线的宽度；值大，暗带更宽 |
| CRT Scanline Scroll Speed | `-0.6–0.6` | `0` 时静止；正负值控制相反的上下移动方向 |
| CRT Red / Blue Offset | `0.0003–0.0012` | 红、蓝通道左右分离；过大时文字和轮廓会重影 |

CRT 是“显示设备”风格，而 Unrecord 式执法记录仪更像现代传感器。想做执法记录仪时可以设为 `0` 或只用 `0.03–0.08`；想做复古监视器画面时再提高到上表范围。

### 5.10 CRT 红蓝偏移：与镜头色差有什么不同

Shader 原本已有的 `_ChromaticAberration` 是**径向色差**：越远离屏幕中心，红蓝偏移越大，模拟镜头边缘无法让三种颜色完全聚焦。

新增的 `_CrtRedBlueShift` 则是**恒定的水平偏移**，模拟老式 CRT/复合视频信号的彩色串扰：绿色保持原采样位置，红色向右、蓝色向左从源画面取样。

```shader
float2 crtRedBlueOffset = float2(_CrtRedBlueShift, 0.0);

colour.r = tex2D(
    _MainTex,
    saturate(uv + colourOffset + crtRedBlueOffset)
).r;
colour.b = tex2D(
    _MainTex,
    saturate(uv - colourOffset - crtRedBlueOffset)
).b;
```

两者可同时使用：径向色差负责“镜头边缘”，红蓝偏移负责“整个显示器的信号特性”。不过先将两者都设小；它们都会增加红蓝通道的额外纹理采样，且数值过大会让 UI 和文字难以阅读。

### 5.11 致幻波动：改变采样坐标，而不是叠一张透明动画

“吃菌子”的画面感核心不是把图像半透明地晃动，而是让屏幕上的每个像素从**略微错误的位置**读取原始画面。Shader 中的 `uv` 就是“去源画面哪里取颜色”的坐标，因此在采样前改变 `uv` 就能产生流动扭曲。

```shader
float waveTime = _Time.y * _HallucinationWaveSpeed;
float radialWave = sin(
    radius * _HallucinationWaveFrequency * 6.2831853 -
    waveTime * 3.0
);
float2 radialWaveOffset = direction * radialWave *
    _HallucinationWaveIntensity * radius;

float2 driftWaveOffset = float2(
    sin((i.uv.y * _HallucinationWaveFrequency * 2.0 +
         waveTime) * 6.2831853),
    cos((i.uv.x * _HallucinationWaveFrequency * 1.6 -
         waveTime * 0.8) * 6.2831853)
) * _HallucinationWaveIntensity * 0.25;

uv = saturate(uv + radialWaveOffset + driftWaveOffset);
```

这段由两个部分叠加：

- `radialWaveOffset`：沿着“屏幕中心 → 当前像素”的方向扩张、收缩，形成呼吸感的同心波纹；乘上 `radius` 后，中心相对稳定，越靠近边缘越容易被拉动。
- `driftWaveOffset`：横纵方向以不同频率、不同速度缓慢漂移，避免波纹过于规则，增加不稳定的流动感。

新增 Inspector 参数：

| 参数 | 建议范围 | 视觉意义 |
| --- | ---: | --- |
| Hallucination Wave Intensity | `0.003–0.015` | 扭曲位移幅度；`0` 为关闭，超过 `0.02` 会很强烈 |
| Hallucination Wave Frequency | `0.8–2.5` | 屏幕上波纹数量；更高会像水面抖动 |
| Hallucination Wave Speed | `0.3–1.5` | 波纹移动/呼吸速度；太快会让人不适 |

建议将该效果只用于短暂的受伤、眩晕、毒雾或剧情状态，并通过脚本把强度从 `0` 平滑升到目标值、再平滑回落。它是可读性成本很高的效果，不应作为普通移动时的常驻镜头效果。

### 5.12 屏幕像素化：让一个采样代表一整块屏幕区域

像素化不是降低游戏窗口分辨率，而是在 Shader 中将 UV 坐标对齐到一个更粗的网格。网格内所有屏幕像素都从同一个网格中心取色，看起来就成为一个大像素块：

```shader
float2 PixelateUv(float2 uv)
{
    float blockSize = max(floor(_PixelationBlockSize + 0.5), 1.0);
    float2 blockCount = max(
        floor(_ScreenParams.xy / blockSize),
        float2(1.0, 1.0)
    );
    float2 pixelIndex = min(
        floor(saturate(uv) * blockCount),
        blockCount - 1.0
    );
    return (pixelIndex + 0.5) / blockCount;
}
```

`_PixelationBlockSize` 的单位是“屏幕像素”：

```text
1  = 每个屏幕像素独立采样，等于关闭像素化
4  = 4 × 4 屏幕像素共用一个颜色，轻微复古
8  = 明显像素块，接近示例图的基础观感
16 = 强烈低分辨率风格
```

主颜色和红蓝通道都使用 `PixelateUv(...)`，所以在打开像素化时不会出现“主画面是方格、红蓝色差却仍然平滑”的不一致问题。

> 示例图二除了像素化，还包含较少的颜色等级和点阵/抖动颗粒。当前实现只完成“方格取样”这一层，保留原来的完整颜色；这样不会突然毁掉场景的光照和可读性。若要完整复刻图二，下一步再加可开关的调色板量化与 Bayer 抖动。

### 5.13 最终输出为什么要保留 Alpha

```shader
return float4(saturate(colour), sourceColour.a);
```

- `saturate(colour)`：最终 RGB 限制在可显示范围 `0–1`；
- `sourceColour.a`：保留源画面的 Alpha；
- 不建议总是写死 Alpha 为 `1`，因为以后若接入需要透明信息的渲染流程，保留源 Alpha 更稳妥。

---

## 6. 调参顺序：先验证，再叠加

一次同时拉五个滑条，几乎无法判断哪个参数造成问题。推荐严格按下面流程：

1. **全为 0**：确认组件开启时画面与关闭时一致；
2. **Lens Distortion**：先调到 `0.04–0.07`；
3. **Vignette**：从 `0.15` 开始，通常 `0.20–0.35` 已有执法记录仪边缘感；
4. **Chromatic Aberration**：`0.0005–0.0015`，只要边缘略有彩边即可；
5. **Noise Intensity**：`0.006–0.018`，先保证远处物体仍然清楚；
6. **Scanline Intensity**：`0.005–0.02`，这是移动传感器扫描，通常可比噪点更低；
7. **CRT Scanline Intensity**：若做复古显示器，再从 `0.08` 开始；执法记录仪风格可保持 `0`；
8. **CRT Scanline Scroll Speed**：从 `0.2–0.4` 开始；负值可反向；
9. **CRT Red / Blue Offset**：从 `0.0005` 开始；不够明显再一点点增加；
10. **Hallucination Wave Intensity**：从 `0.003` 开始，确认玩家仍能读清环境后再提高；
11. **Pixelation Block Size**：从 `4` 开始；想接近低分辨率图像可试 `8–12`；
12. 最后再调 `BodycamSway` 的旋转和位移。

一组可作为起点的“轻量执法记录仪”参数：

| 参数 | 建议起点 |
| --- | ---: |
| Lens Distortion | `0.05` |
| Vignette | `0.25` |
| Chromatic Aberration | `0.0008` |
| Noise Intensity | `0.01` |
| Scanline Intensity | `0.01` |
| CRT Scanline Intensity | `0` |
| CRT Scanline Density | `240` |
| CRT Scanline Thickness | `0.12` |
| CRT Scanline Scroll Speed | `0` |
| CRT Red / Blue Offset | `0` |
| Hallucination Wave Intensity | `0` |
| Hallucination Wave Frequency | `1.8` |
| Hallucination Wave Speed | `1` |
| Pixelation Block Size | `1` |
| Smooth Speed | `10–14` |
| Max Turn Roll | `1.5–2.5` |

**调参原则：** 真实感不是“所有效果都最大”。玩家首先需要看清目标、道路和交互物；效果应只在他没有刻意观察时被感觉到。

---

## 7. 多个图像效果的顺序

本相机还有 `CameraBloodEffect`。多个使用 `OnRenderImage` 的组件都挂在同一台相机时，Inspector 中组件的先后顺序会影响结果。

常见选择是：

```text
场景画面 → 血迹效果 → Bodycam 效果 → 屏幕
```

这样血迹也会带一点镜头噪点和边缘畸变，像是真实摄像头拍到的污渍。

如果把顺序反过来：

```text
场景画面 → Bodycam 效果 → 血迹效果 → 屏幕
```

血迹会显得过于干净、像最后叠上的 UI。两种都可以，但必须有意识地选择，而不是让组件顺序碰巧决定画面。

---

## 8. 常见故障排查

### 8.1 开启组件后整张画面灰、黑或异常

按此顺序排查：

1. 先把五个强度全部设为 `0`；
2. 右键 `BodycamPost.shader`，选择 **Reimport**；
3. 确认 `BodycamPostEffect` 的 Shader 字段是 `DeadEarth/BodycamPost`；
4. 确认该项目仍是 Built-in Pipeline，而不是 URP；
5. 确认 `OnRenderImage` 中保留了中间 `RenderTexture` 的复制流程；
6. 暂时禁用其他图像效果，判断是否是效果之间的顺序/兼容问题。

最有价值的定位方法是做“最小测试”：让 Shader 临时直接返回源色。若画面仍异常，问题在渲染链路；若恢复正常，问题在某个像素计算。

### 8.2 暗角不明显

检查两件事：

- Inspector 的 `Vignette` 是否至少大于 `0.15`；
- Shader 是否仍使用当前的 `colour *= 1.0 - edge * _Vignette`。

不要靠增加噪点来伪造暗角；两者是不同的视觉语言。

### 8.3 画面太灰、看不清

优先降低：

1. `Noise Intensity`；
2. `Scanline Intensity`；
3. `Vignette`；
4. 色差。

然后检查场景本身是否过暗。后处理不能补救没有光照的场景；它只是在已有画面上做风格化。

### 8.4 镜头晃得头晕

先降低 `_turnRollPerDegreePerSecond` 和 `_maxTurnRoll`，再降低横移/前进的位移系数。不要先盲目提高 `_smoothSpeed`：它会减少滞后，却不一定解决幅度过大的问题。

### 8.5 相机静止后位置不对

检查 `BodycamSway` 的 Transform 是否原本有非零位置或旋转。若有，应按第 3.7 节缓存基础姿势，不要让脚本强行回到零。

---

## 9. 下一步可以如何扩展

当基础效果稳定后，可一次只扩展一个方向：

- **更真实的噪点**：用蓝噪点贴图替代数学伪随机噪点；
- **低光模式**：只在暗处提高噪点，亮处维持干净；
- **屏幕信息层**：录制时间、电池、红点 REC、编号；这类应做成 Canvas/UI，而不是塞进当前后处理 Shader；
- **跑步状态加强**：从 `FPSController` 的走/跑状态读取额外强度，而不是只看速度；
- **着地冲击**：订阅 `FPSController.HeavyLanded`，在落地时给 `BodycamSway` 一个短暂的下沉脉冲；
- **URP 迁移**：把 `OnRenderImage` 的桥梁替换为 Scriptable Renderer Feature，Shader 的 UV/像素数学大部分仍可复用。

---

## 10. 最终检查清单

- [ ] `BodycamSway` 是 `FPS Camera` 的父物体；
- [ ] `BodycamSway` 挂有 `BodycamSway.cs`；
- [ ] 真正有 Camera 组件的物体挂有 `BodycamPostEffect.cs`；
- [ ] Shader 字段指向 `DeadEarth/BodycamPost`；
- [ ] 所有参数为 0 时画面仍正常；
- [ ] 每次只提高一个参数并观察；
- [ ] 多个图像效果的组件顺序经过确认；
- [ ] 场景中有未保存改动时，确认满意后再保存；
- [ ] 改完外部 Shader 文件后，必要时在 Unity 中 Reimport。

如果你能清楚回答下面三个问题，就已经掌握了这套系统的核心：

1. 为什么 `BodycamSway` 放在相机父物体上，而不是直接挂在相机？
2. `source` 和 `destination` 在 `OnRenderImage` 中分别代表什么？
3. 为什么将速度从世界坐标转换到玩家局部坐标后，横移效果才不会因朝向改变而出错？

答案分别是：避免与上下看抢旋转；源画面与最终输出；以及“向右横移”的含义必须相对于玩家自身，而不是固定世界坐标。
