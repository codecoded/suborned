# Third-person chase cameras: follow, recentre, turn-around, collision and occlusion

Context: a PC (mouse and keyboard) real-time tactics game where the player can drop into one squad agent at street level. The designer's complaint is that the camera should respect the direction of travel: on a turn-around, it should let the agent move that way and then pan round slowly. Today the camera swings behind the agent's heading in about 0.5 s, at a constant distance and pitch.

Source key:
- **Primary** means engine source code, official docs, or a practitioner writing about their own shipped work.
- **Secondary** means a blog or a summary.
- **Anecdotal** marks a forum post, a mod, or one hobby project's issue thread.

---

## 1. Lazy (spring/damped) follow vs rigid behind-the-back camera

### Takeaway
A rigid behind-the-back camera is locked to the character's facing (yaw lag zero or near zero). It suits aiming and over-the-shoulder shooting. A lazy camera follows **position** but not necessarily **orientation**: it keeps distance and height and turns as little as it can. Most exploration cameras sit between the two, with damped position, damped or partial yaw follow, and a dead zone.

### Cited Findings
- Haigh-Hutchinson's *Real-Time Cameras* names two behind-the-character variants. In one, "the position of the camera is fixed with respect to the character orientation". In the other, it "may have rotational lag around the vertical axis of the character". Over-the-shoulder views "typically carry little or no orientation lag" and are used in hybrid first/third-person styles to help aiming. — [epdf.pub excerpt of Real-Time Cameras (via search snippet)](https://epdf.pub/real-time-cameras.html). Secondary access to a primary text: I saw only search snippets, not the full chapter.
- Haigh-Hutchinson's GDC 2005 slides list "orientation lag" as one camera-orientation behaviour, alongside look-at position, object tracking, object framing, path tracking and free-look. — [GDC 2005, Fundamentals of Real-Time Camera Design (slides)](https://media.gdcvault.com/gdc05/slides/GD_Haigh-Hutchinson_FundamentalsReal-TimeCameraDesign2.pdf). The PDF's fonts could not be decoded, so this is via the search summary.
- The book also argues for vertical positional lag "to dampen the effect of the player walking over small obstructions". — [epdf.pub excerpt (via search snippet)](https://epdf.pub/real-time-cameras.html)
- Cinemachine 3 `OrbitalFollow` / `Follow` binding modes make the distinction explicit:
  - **Lock To Target** "uses the target's full frame, so the camera rotates with the target". This is the rigid behind-the-back mode.
  - **Lock To Target With World Up** follows yaw only.
  - **World Space** means "the camera doesn't move when the target rotates".
  - **Lazy Follow**: "Offset and damping are interpreted in camera-local space. The camera moves as little as possible to keep the same distance and height from the target, regardless of the target's orientation."
  - Source: [Unity Cinemachine 3.1 manual, Orbital Follow](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html)
- Eric Martel (GameAI Pro, ch. 47; he describes the game only as "one of the most successful action adventure games") separates two camera types:
  - **Orbit camera:** the player controls azimuth and polar angle, and the camera returns behind the character only when the player isn't steering it.
  - **Over-the-shoulder camera:** it "always follows the character's facing" and "mostly reacts like a first-person camera that had been moved behind the character by a couple of meters".
  - Source: [Martel, "Tips and Tricks for a Robust Third-Person Camera System", Game AI Pro (2013)](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf). Primary.
- A research paper on camera placement notes that a third-person camera typically follows the avatar's movement "but not necessarily its orientation, since that might induce motion sickness to the player". — [arXiv 2109.03750, "How Camera Placement Affects Gameplay in Video Games"](https://arxiv.org/pdf/2109.03750) (via search snippet)
- Nesky's list (as summarised by Skillbox) includes these rules:
  - Attach the camera to the character and control it by distance, not by global coordinates (#3).
  - "Add camera lag and inertia, and cap how far the avatar can drift from screen center" (#40).
  - Rigidly locking the camera to the avatar during jumps "creates two jarring jumps"; transition smoothly (#45).
  - Moving the camera too fast causes motion sickness (#46).
  - Source: [Skillbox, "50 principles for a good game camera" (Russian summary of Nesky's GDC 2014 talk)](https://skillbox.ru/media/gamedev/50-principov-vazhnyh-dlya-sozdaniya-horoshey-igrovoy-kamery/). Secondary and translated.
- Solar Ash (from Little Polygon's breakdown):
  - Smooths the tracking point vertically only, "but no horizontal smoothing which would cause the camera to lag as Rei zipped around at 45MPH".
  - Projects the player onto the ground so jumps don't move the camera.
  - Suggests a low-pass filter or critically damped spring on the tracked position to remove small bumps.
  - Source: [Max Kaufmann, "Tech Breakdown: Third Person Cameras in Games", Little Polygon blog (2023-01-14)](https://blog.littlepolygon.com/posts/cameras/). Practitioner blog.
- Itay Keren separates "lerp smoothing" (each frame closes a fraction of the gap, which is ease-out) from "physics smoothing" (the camera accelerates and decelerates, which is ease-in-out). He recommends SmoothDamp-style springs where available. — [Itay Keren, "Scroll Back: The Theory and Practice of Cameras in Side-Scrollers", Game Developer (GDC 2015 talk)](https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers). Primary. Keren says "many of these general concepts apply to 3D as well".

### Inferences
- A rigid swing behind the heading in 0.5 s is close to Haigh-Hutchinson's "fixed with respect to character orientation" or over-the-shoulder class. That class suits aiming, not exploration on foot. The designer's complaint matches what the sources describe: an exploration camera should follow position and turn lazily.
- Lazy Follow is the reference design for "respect the direction I'm going". In this mode, walking toward the camera pushes the camera back rather than spinning it.
- Splitting position smoothing (vertical heavy, horizontal light, as in Solar Ash) from yaw smoothing avoids the camera losing the character at speed. For a walking or running agent at street scale, horizontal position lag can be small.

### Gaps
- I could not read Haigh-Hutchinson's chapter text directly, so I have no numeric lag values from the book. The book is noted for describing behaviour rather than prescribing numbers.

---

## 2. Auto-recentre: when, how fast, delay after mouse input, and the 180° turn / walking toward the camera

### Takeaway
Shipping practice and engine defaults agree on three rules:
1. Never recentre while the player is steering the camera.
2. Wait about 1 s after the last camera input.
3. Recentre over about 2 s, damped rather than linear, and only while the character is moving.

For a turn-around, the strongest pattern is a "leash": turn the camera only by the **sideways** part of the character's motion, so running straight toward or away from the camera doesn't turn it. The camera ends up behind the character only if they keep going that way. The alternative, a recentre whose speed scales with the angle, is exactly what makes a 180° turn whip the camera round.

### Cited Findings
- **Cinemachine 3 recentering defaults** (engine source, `InputAxis.RecenteringSettings.Default`):
  - `Enabled = false`, `Wait = 1` s, `Time = 2` s.
  - Wait tooltip: "If no user input has been detected on the axis for this many seconds, re-centering will begin."
  - Time tooltip: "How long it takes to reach center once re-centering has started."
  - Source: [Cinemachine source, InputAxis.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Core/InputAxis.cs). Primary.
- **The Cinemachine recentering algorithm is damped, not linear.** It calls `Mathf.SmoothDamp(v, c, ref velocity, Recentering.Time * 0.5f, 9999, deltaTime)`, so the smooth time is half of Time and the speed is effectively uncapped. It starts once `CurrentTime - LastValueChangeTime >= Recentering.Wait`. — [InputAxis.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Core/InputAxis.cs)
- **Cinemachine recentering target:** `OrbitalFollow.RecenteringTarget` defaults to `TrackingTarget`, where the "center moves behind the Tracking Target's forward direction". The other options:
  - **Axis Center:** static.
  - **Parent Object.**
  - **LookAt Target.**
  - Sources: [OrbitalFollow.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineOrbitalFollow.cs); [Orbital Follow manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html)
- **Martel (shipped action-adventure):** "any time that the player is not actively rotating the camera and the character is in motion, it is better to blend the azimuthal angle back to 0 (behind the character) and the polar angle to some default value." Recentring is gated on **both** no camera input **and** the character moving. — [Martel, Game AI Pro ch. 47](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Martel's sweet spot:** the camera rotates on its own only when the target leaves a screen-space rectangle. "When the player was actively rotating the camera, we would simply recenter the view on its target and disable the sweet spot." — [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Martel on changing input reference frames:**
  - With continuous camera rotation, "the player is typically able to adapt and gradually adjusts the input".
  - On a cut, keep using the previous view's matrix for movement input "as long as the input is held", so movement doesn't reverse ("ping-pong").
  - Source: [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Leash (Little Polygon):** "The vertical component of the cross product between view direction and player velocity is scaled and added to yaw, creating a 'leash' effect." — [Kaufmann, Little Polygon (2023)](https://blog.littlepolygon.com/posts/cameras/)
- **Turn-around problem, worked through in an open-source game (LBA2 community; anecdotal, one project):**
  - The existing return pulls the pan to 0 with an exponential time constant, after a `cam_recenter_delay` of 60 steps ("about a second of walking"). The pull "takes about three time constants, under a second".
  - Because the pull is a share of the remaining angle, "a large offset swings faster. Turning round and running toward the camera swings it through 180 degrees quickly". The author says this reads as "the camera fixing itself to his back".
  - The proposed "leash" turns the camera by the sideways part of the motion divided by distance:
    - Running across the view turns the camera steadily toward the path, about 11°/s at their arm length.
    - "Running toward or away from the camera does not turn it."
    - "Standing or turning on the spot leaves it where it is."
    - "It only ends up behind him if he keeps moving the same way."
    - The author calls it "the classic third-person follow", with Ocarina of Time as the usual reference.
  - Source: [LBALab/lba2-classic-community issue #732](https://github.com/LBALab/lba2-classic-community/issues/732)
- **Dark Souls-style push (anecdotal, forum):** "When you walk towards the camera you push it out of the way, and later on drag it behind you." — [Unreal forums, "Third Person Camera that aligns to movement direction"](https://forums.unrealengine.com/t/third-person-camera-that-aligns-to-movement-direction/1642)
- **Nesky:**
  - "Player input comes first" (#9).
  - "If the player is inspecting something, don't pull their focus away with camera movement" (#35).
  - Let the player take manual control even in tutorials (#36).
  - When running forward, the camera usually aligns to show what's ahead, but there are exceptions (#24).
  - Source: [Skillbox summary of Nesky](https://skillbox.ru/media/gamedev/50-principov-vazhnyh-dlya-sozdaniya-horoshey-igrovoy-kamery/). Secondary.
- **Keren's dual forward focus** keeps the camera's target "until Mario hits a predefined threshold" (Super Mario World), so small direction changes don't flip the camera. This is hysteresis against reversals. — [Keren, Scroll Back](https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers)

### Inferences
- **Effective Cinemachine default timing.** SmoothDamp approximates a critically damped spring with ω ≈ 2/smoothTime. With Time = 2, smoothTime = 1 s and ω ≈ 2/s. From rest, the remaining fraction is (1+ωt)e^(−ωt):
  - about 60% remains at 1 s;
  - about 9% remains at 2 s, so "Time" ≈ time to ~90% done.
  - Default behaviour is therefore: nothing for 1 s after the last mouse input, then about 2 s to swing most of the way round. That is roughly 4× slower than the current 0.5 s, plus a 1 s hold. (My arithmetic from the cited code.)
- **Why the current camera feels wrong.** Any return toward "behind the facing" whose rate grows with the angle (exponential, lerp or SmoothDamp) is fastest exactly when the agent turns 180°. That is the designer's complaint. The leash avoids it by construction:
  - Yaw rate = (lateral speed relative to the view) / (arm length), in rad/s.
  - Walking toward the camera gives zero turn, so the player sees the agent's face and keeps moving.
  - As soon as the path has any sideways part, the camera pans round at a rate set by speed, not by the angle offset.
  - Example: walking at 1.4 m/s fully sideways with a 5 m arm gives 0.28 rad/s ≈ 16°/s; running at 4 m/s gives ≈ 46°/s. Add a gain (0.5–1.0) to taste. These are illustrative numbers, not sourced.
- **Suggested hybrid that fits the brief** ("let the player be moving in that direction and then slow pan around"):
  1. Leash as the base.
  2. An optional slow behind-recentre that runs only after the agent has held one heading for 1–2 s, not during the turn.
  3. Both suppressed for about 1 s after any mouse camera input (Cinemachine's Wait).
  4. Recentre also suppressed while the agent stands still (Martel).
  5. A hysteresis threshold (Keren) so small zig-zags don't start a swing.
- **For a tactics game where the player may be watching something,** Nesky's #35 argues for not auto-turning when the agent is stationary or the player is aiming or inspecting.

### Gaps
- No primary source found for Journey's camera specifically (Nesky's own writing could not be located), or for Naughty Dog, Santa Monica or Insomniac numeric recentre values.
- No shipped-game value for the "hold a heading for N seconds before recentring" threshold. The LBA2 one-second delay is a hobby project's choice.
- No source settles "hold so the player sees the face" vs "swing" for walking toward the camera with numbers. The sources (LBA2 leash, the Dark Souls push, Cinemachine Lazy Follow) all point to *not* swinging, but that is a pattern, not a quoted rule.

---

## 3. Look-ahead and leading the frame

### Takeaway
Leading the frame means either offsetting the aim point ahead of the character (direction- or velocity-based) or framing the character off-centre opposite the travel direction. 2D practice gives about 25% of screen width as a classic lead. Engine look-ahead is velocity-projection, measured in tenths of a second, and is noise-sensitive. Use hysteresis so reversals don't jerk the frame.

### Cited Findings
- Keren's examples:
  - Defender keeps focus "around 25% screen-width forward".
  - Super Mario Bros. uses a point about 25% off-centre.
  - Bonanza Bros. moves its focus at about double the player's speed.
  - Projected focus aims at the extrapolated position. Mushroom 11 projects a few ms ahead and scales smoothing with speed. Projection works poorly around jumps and landings.
  - Source: [Keren, Scroll Back](https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers)
- **Cinemachine look-ahead** (`LookaheadSettings`, used by Rotation Composer and Position Composer). Defaults are `Enabled = false`, `Time = 0`, `Smoothing = 0`, `IgnoreY = false`.
  - Time is in seconds, range 0–1: "Predict the position this many seconds into the future". The code warns it is "sensitive to noisy animation".
  - Smoothing has range 0–30; larger values "smooth out jittery predictions and also increase prediction lag".
  - Source: [Cinemachine source, LookaheadSettings.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Core/LookaheadSettings.cs)
- **Cinemachine Rotation Composer defaults:** Damping (0.5, 0.5); "Small numbers are more responsive, rapidly orienting the camera to keep the target in the dead zone". `CenterOnActivate = true`. — [CinemachineRotationComposer.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineRotationComposer.cs)
- **Nesky:**
  - When running forward the camera aligns to show what's ahead (#24).
  - Don't aim forward over a drop; raise the camera (#25).
  - Raise the camera when something important is directly ahead (#22).
  - Rotate toward a distant point of interest rather than zoom out (#34).
  - Source: [Skillbox summary](https://skillbox.ru/media/gamedev/50-principov-vazhnyh-dlya-sozdaniya-horoshey-igrovoy-kamery/)
- **Little Polygon framing:** use viewport coordinates −1..1. Tiny Starpilot uses counter-framing: "aiming right places the player on the left". — [Kaufmann (2023)](https://blog.littlepolygon.com/posts/cameras/)
- **Arkham Asylum** (blog, secondary): the camera is offset while exploring, centres and pulls out in combat, and centres while sprinting "to give the player more view ahead". — [Andreas Buehler, "Third Person Camera View in Games…", Game Developer (2019-11-11)](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)

### Inferences
- In third person, the leash already leads the frame: the camera turns toward the path, so space opens in the direction of travel.
- A small velocity look-ahead on the aim point (Cinemachine-style, about 0.2–0.4 s with smoothing, IgnoreY on) can add lead without turning the camera. These values are not sourced.
- Street-level agents have noisy root motion, so use the smoothed velocity, not raw animation (Cinemachine's own warning).

### Gaps
- No primary 3D source gives a numeric look-ahead distance in metres for on-foot third person.

---

## 4. Dead zones, soft zones, damping and recentring settings in the engines (with shipped defaults)

### Takeaway
Engines expose the same knobs:
- **Dead zone:** no correction while the target is inside it.
- **Soft zone / hard limits:** damped correction between the dead zone and a hard edge the target can't cross.
- **Per-axis position damping**, measured in seconds (Unity) or as an interp speed (Unreal).
- **Recentre wait and time.**

Defaults usually leave the dead zone, look-ahead and recentring **off**, so they have to be tuned on purpose.

### Cited Findings: Unity Cinemachine 3 (source-verified defaults)
- **Screen composition** (`ScreenComposerSettings.Default`):
  - DeadZone `Enabled = false`, `Size = (0.2, 0.2)` (full screen = 1). "The camera will not adjust if the target is within this range of the screen position."
  - HardLimits `Enabled = false`, `Size = (0.8, 0.8)`. "The target will not be allowed to be outside this region. When the target is within this region, the camera will gradually adjust… depending on the damping speed." This hard-limit region is Cinemachine 3's equivalent of the older "soft zone".
  - Source: [ScreenComposerSettings.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Core/ScreenComposerSettings.cs)
- **Rotation Composer:** Damping (0.5, 0.5), Lookahead off. — [CinemachineRotationComposer.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineRotationComposer.cs)
- **Third Person Follow** (`Reset()`):

  | Setting | Default |
  |---|---|
  | Damping (x, y, z) | 0.1, 0.5, 0.3 |
  | ShoulderOffset | (0.5, −0.4, 0) |
  | VerticalArmLength | 0.4 |
  | CameraSide | 1 (right) |
  | CameraDistance | 2.0 |
  | AvoidObstacles: Enabled | false |
  | AvoidObstacles: CameraRadius | 0.2 |
  | AvoidObstacles: DampingIntoCollision | 0 |
  | AvoidObstacles: DampingFromCollision | 0.5 |

  Source: [CinemachineThirdPersonFollow.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineThirdPersonFollow.cs). The manual describes the same fields but gives no defaults: [Third Person Follow manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html).
- **Orbital Follow:**

  | Setting | Default |
  |---|---|
  | OrbitStyle | Sphere |
  | Radius | 10 (field initializer); `Reset()` sets 5 |
  | Three Ring: Top | height 5, radius 2 |
  | Three Ring: Center | height 2.25, radius 4 |
  | Three Ring: Bottom | height 0.1, radius 2.5 |
  | SplineCurvature | 0.5 |
  | HorizontalAxis | range −180..180, wraps |
  | VerticalAxis | value and centre 17.5°, range −10..45 |
  | RecenteringTarget | TrackingTarget |

  Source: [CinemachineOrbitalFollow.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineOrbitalFollow.cs)
- **Recentering:** Enabled false, Wait 1 s, Time 2 s, SmoothDamp with smoothTime = Time/2. — [InputAxis.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Core/InputAxis.cs)
- **Damping semantics** (manual): "smaller values respond faster". Position Damping is per axis. Angular damping can be Euler (per-axis Pitch/Roll/Yaw) or Quaternion (single value). — [Orbital Follow manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineOrbitalFollow.html)

### Cited Findings: Unreal Engine Spring Arm
- `CameraLagSpeed` works only if `bEnableCameraLag` is true and controls how quickly the camera reaches the target position. "Low values are slower (more lag), high values are faster (less lag), while zero is instant (no lag)." — [UE 4.26 API, USpringArmComponent::CameraLagSpeed](https://docs.unrealengine.com/4.26/en-US/API/Runtime/Engine/GameFramework/USpringArmComponent/CameraLagSpeed/index.html) (via search snippet)
- `TargetArmLength` is the "Natural length of the spring arm when there are no collisions". `ProbeSize` is "How big should the query probe sphere be (in unreal units)", used with `ProbeChannel` when `bDoCollisionTest` is on. — [UE 4.26 USpringArmComponent API](https://docs.unrealengine.com/4.26/en-US/API/Runtime/Engine/GameFramework/USpringArmComponent/) (via search snippet)
- Epic's C++ PlayerCamera tutorial sets `TargetArmLength` to 400 and `CameraLagSpeed` to 3.0 with `bEnableCameraLag = true`. These are tutorial values, not engine defaults. — [UE 4.26 PlayerCamera tutorial](https://docs.unrealengine.com/4.26/en-US/ProgrammingAndScripting/ProgrammingWithCPP/CPPTutorials/PlayerCamera) (via search snippet)
- A community tuning guide warns that heavy position and rotation lag together "feel mushy". — [Bugnet, "How to fix Unreal spring arm camera lag"](https://bugnet.io/blog/how-to-fix-unreal-spring-arm-camera-lag-feels-laggy). Secondary, anecdotal.

### Cited Findings: practitioner patterns
- Martel's "sweet spot":
  - It is a screen-space rectangle; the camera rotates on its own only when the target leaves it. "The size of the zone can be adjusted to absorb bigger movements, such as crouching or jumping, or focused down to make the camera feel more dynamic."
  - It is implemented by converting the rectangle into angles and testing with two dot products.
  - It is also used to frame the nearest *n* enemies in fights: the smallest heading change places each in the sweet spot, and the player's character is adjusted last.
  - Source: [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Camera transitions (Martel):**
  - Blend position, orientation and FOV linearly.
  - Cut when the views differ by more than 90°.
  - Use the minimum of the outgoing and incoming blend times.
  - Pass the previous camera's state to the new camera so the centre of view matches.
  - Source: [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Keren:** the camera window (dead zone) means "only scroll when it pushes against the edges of that window", which removes needless motion. Position snapping slowly reduces drift inside the window. — [Keren, Scroll Back](https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers)

### Inferences
- **Mapping Unreal lag to seconds.** UE's lag uses `VInterpTo` / `RInterpTo`, which per frame move a fraction ≈ clamp(dt × speed, 0, 1) of the remaining gap. That is approximately exponential with time constant ≈ 1/speed:
  - speed 10 ≈ 0.1 s time constant (≈ 95% in 0.3 s);
  - speed 3 ≈ 0.33 s (≈ 95% in 1 s).
  - This is from my knowledge of the engine's interp functions, not fetched this session; see Gaps.
- **Unity damping is roughly "seconds to settle"**, and smaller is snappier. Cinemachine's Third Person Follow default of 0.1 s side / 0.5 s vertical / 0.3 s forward is a reasonable starting point for position lag.
- The current 0.5 s yaw swing is similar to Unreal rotation lag speed 2–6 (time constant 0.17–0.5 s), which is fairly snappy.

### Gaps
- **Unreal Spring Arm constructor defaults could not be verified this session.** The source and the 4.27/5.x doc pages returned 404/403 or empty, and no page with the exact values was found. From recollection of `SpringArmComponent.cpp`, *unverified*:
  - `TargetArmLength = 300` (cm)
  - `ProbeSize = 12`
  - `ProbeChannel = ECC_Camera`
  - `bDoCollisionTest = true`
  - `bEnableCameraLag = false`
  - `bEnableCameraRotationLag = false`
  - `CameraLagSpeed = 10`
  - `CameraRotationLagSpeed = 10`
  - `CameraLagMaxTimeStep = 1/60`
  - `CameraLagMaxDistance = 0` (no cap)
  - `bUseCameraLagSubstepping = true`

  Check these against the engine header before quoting them.
- The Cinemachine 2.x FreeLook defaults (legacy) were not fetched. They differ from CM3's OrbitalFollow; the CM3 values above are the current ones.

---

## 5. Typical distance, height, pitch, FOV and shoulder offset: exploration vs aiming

### Takeaway
Engine defaults put an exploration orbit at about 4–5 m with the camera about 2.25 m up and a default pitch of about 17.5°. They put an aiming or over-the-shoulder rig at about 2 m, with the camera 0.5 m to the right of and 0.4 m below the pivot. Practitioners vary distance with pitch: closer when looking up, further when looking down. They also prefer changing distance to changing FOV for comfort. I found no sourced FOV numbers.

### Cited Findings
- **Cinemachine Orbital Follow defaults** (exploration orbit): centre ring height 2.25 m, radius 4 m; top 5 m / 2 m; bottom 0.1 m / 2.5 m; sphere radius 5 m (Reset) or 10 m (initializer); default vertical angle 17.5°, range −10° to 45°. — [OrbitalFollow.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineOrbitalFollow.cs)
- **Cinemachine Third Person Follow defaults** (aiming / over-the-shoulder): distance 2 m, shoulder offset (0.5, −0.4, 0), vertical arm 0.4 m, right shoulder. — [CinemachineThirdPersonFollow.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineThirdPersonFollow.cs)
- **Martel (shipped game):**
  - The over-the-shoulder camera sits "behind the character by a couple of meters".
  - The orbit camera uses a spline on the vertical axis instead of constant distance: "a closer shot for a low-angle and moving the camera away for a high-angle shot will give better results."
  - Close targets make the shoulder offset cause aim parallax; fix it with aim IK.
  - Source: [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Nesky:**
  - Keep the camera close, since distance raises the chance of occlusion (#5).
  - Distance should vary nonlinearly with view angle: closer looking up, further looking down (#14).
  - Widen FOV when looking up (#15).
  - Adjust distance, FOV and tilt together (#16).
  - A very low FOV makes the world rush past and causes sickness (#41).
  - Dynamic FOV changes cause discomfort; offer an option (#42).
  - Source: [Skillbox summary](https://skillbox.ru/media/gamedev/50-principov-vazhnyh-dlya-sozdaniya-horoshey-igrovoy-kamery/)
- **Little Polygon:** pull out for action, in for narrative moments. Clamp pitch; wrap yaw. Advises against FOV changes "for dramatic effect because it can make players sick"; prefer distance changes. — [Kaufmann (2023)](https://blog.littlepolygon.com/posts/cameras/)
- **Exploration vs combat** (blog, secondary): Arkham Asylum offsets the camera while exploring and pulls out and centres for combat. Elder Scrolls Online's right-offset shoulder camera misaligns the centre-screen target with the line of sight, so players miss doors and opponents. — [Buehler, Game Developer (2019)](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)

### Inferences
- For a street-level agent in a tactics game:
  - **Exploration/follow:** about 4–6 m, pivot about 1.6–2 m, pitch about 15–20° down, little or no shoulder offset (centred, so doors and people aren't misread).
  - **Aim:** about 2 m, 0.5 m shoulder offset.
  - These are interpolated from the defaults above, not shipped-game numbers.

### Gaps
- No primary source with shipped FOV values for third-person exploration vs aim. Both engines' default camera FOVs exist but were not verified this session.
- No numbers from Naughty Dog, Santa Monica or Insomniac talks were found.

---

## 6. Collision and occlusion: pull-in, fading, buildings blocking the view

### Takeaway
There are three families: **pull in** along the arm (spring arm), **move around** (whiskers, or sliding along the wall normal), and **show through** (fade, dither or cut out the occluder, or draw a silhouette).

Shipped practice treats player-driven and autonomous cases differently:
- **Player-driven:** keep the player's angle and shorten the arm; let the player occlude if they choose.
- **Autonomous follow:** slide the azimuth along the wall rather than snapping in.

Pull-in should be fast (about 0) and recovery slow (about 0.5 s or more) so the camera doesn't pump.

### Cited Findings
- **Martel (shipped game):**
  - **Collision flags** let modellers mark small posts as ignored and foliage as "occluding the view but not blocking camera movement".
  - **Camera size:** they padded the camera with a collision primitive but say "in retrospect… it would have been far simpler just to adjust the near clip".
  - **Player-driven collision:** "our orbit camera would simply reduce its distance to the target… As the player would walk away from the collision, the camera would therefore keep the same point of view and start following at the original distance once the collision is cleared."
  - **Autonomous collision** (most often the player backing toward a wall): "smoothly modify the azimuth angle of the camera in the direction of the collision's normal vector if it would allow the camera to be closer to its desired distance… we avoided oscillation in corners while moving the camera horizontally along the walls."
  - **Occlusion with player input:** do nothing; "if the player wanted to occlude his character, it was his choice and we allowed it."
  - **Occlusion while following:** move the camera opposite to the player's movement, and along the vertical spline for vertical occlusion.
  - Source: [Martel, Game AI Pro ch. 47](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Cinemachine Third Person Follow obstacle settings:**
  - Camera Radius 0.2 m, "how close the camera can get to collidable obstacles without adjusting its position".
  - Damping Into Collision 0, "how gradually the camera moves to correct for an occlusion".
  - Damping From Collision 0.5 s, "how gradually the camera returns to its normal position".
  - Ignore Tag is recommended to be set to the target's tag.
  - Off by default.
  - Sources: [ThirdPersonFollow.cs](https://raw.githubusercontent.com/Unity-Technologies/com.unity.cinemachine/main/com.unity.cinemachine/Runtime/Components/CinemachineThirdPersonFollow.cs); [manual](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineThirdPersonFollow.html)
- **Unreal Spring Arm:** with `bDoCollisionTest`, it sweeps a sphere of `ProbeSize` along `ProbeChannel` from pivot to camera and shortens the arm to the hit. `TargetArmLength` is the natural length "when there are no collisions". — [UE 4.26 USpringArmComponent API](https://docs.unrealengine.com/4.26/en-US/API/Runtime/Engine/GameFramework/USpringArmComponent/) (via search snippet)
- **Nesky:**
  - Raycast and nudge the camera away from tall walls (#6).
  - Snapping close on contact "is a cut"; respect editing rules such as the 30° rule (#7).
  - Tag thin or safe obstacles to be ignored, but keep a collision sphere so the camera isn't inside them (#8).
  - If the player aims into an obstacle, pull the camera in rather than fight the player (#9).
  - Give competing camera algorithms a priority order (#10).
  - Raise the camera on hills and stairs instead of orbiting (#11).
  - With an obstacle behind the character, "move the camera closer rather than swinging it sideways" (#12).
  - When too close, use a proximity trigger and make the avatar semi-transparent (#13).
  - For see-through occluders, a hard cut is a last resort (#17).
  - Source: [Skillbox summary](https://skillbox.ru/media/gamedev/50-principov-vazhnyh-dlya-sozdaniya-horoshey-igrovoy-kamery/)
- **Shipped occlusion treatments** (blog, secondary):
  - The Witcher 3 makes near-camera foliage transparent ("geometry dithering").
  - Super Mario Sunshine draws Mario's silhouette through walls.
  - For Honor lets the camera pass through geometry with an edge indicator ("cut-out view").
  - A Hat in Time uses "camera whiskers" to steer around nearby objects.
  - Problem cases: Assassin's Creed Syndicate's camera hits walls, narrow alleys and NPCs; Dark Souls 3's camera clings to large enemies and bounces in tight spaces, which the author attributes to sphere colliders.
  - Source: [Buehler, Game Developer (2019)](https://www.gamedeveloper.com/design/third-person-camera-view-in-games-a-record-of-the-most-common-problems-in-modern-games-solutions-taken-from-new-and-retro-games)
- **Martel on debugging:** draw failed collision tests as red cylinders that persist for about 5 s, or until cleared, because collision meshes drift from render meshes. — [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)

### Inferences
- **Street level in a city of large buildings:**
  - **NPCs, posts and thin props:** use a camera-ignore layer.
  - **Buildings behind the agent:** pull in fast, with Cinemachine-style into-collision 0 and from-collision about 0.5 s or more.
  - **Agent backed against a facade:** slide the azimuth along the wall normal (Martel).
  - **Facades between camera and agent from an angle:** dither or fade them, or draw a silhouette. The project's "exteriors only" rule makes fade or cut-out cheaper than routing the camera.
- With a leash camera, occlusion from a turn-around is less likely than with a rigid swing, since the camera doesn't sweep through a 180° arc past nearby walls.

### Gaps
- No primary talk with numbers for fade distances or dither thresholds.

---

## 7. Classic camera mistakes and rules from expert talks

### Takeaway
The recurring rules:
- Player input beats automation.
- Avoid unnecessary or abrupt motion.
- Keep the subject and what matters ahead in frame.
- Prefer moving in over swinging sideways around obstacles.
- Never let changes to the camera's reference frame reverse the player's movement.
- Test the camera with many kinds of player.

### Cited Findings
- **Keren's guidelines:**
  - "Keep attention on your control subject."
  - "Avoid unnecessary or unpredictable bumps."
  - "Avoid sudden camera speed and direction changes."
  - "Keep the important details in the frame."
  - "Set up scene cues, context and progression."
  - Source: [Keren, Scroll Back](https://www.gamedeveloper.com/design/scroll-back-the-theory-and-practice-of-cameras-in-side-scrollers)
- **Nesky's list** (via Skillbox, secondary). Most relevant here:
  - The wrong camera type (#1).
  - Collaborate with level design early (#2).
  - Snapping in is a cut (#7).
  - Priority hierarchy for competing behaviours (#10).
  - Move closer rather than swing (#12).
  - A cut mustn't change the character's movement axis (#18).
  - Keep landmarks for dead reckoning (#19).
  - Players use screen centre as a reference, so film rules like the rule of thirds can hurt gameplay (#28).
  - Designer overrides per scene (#30).
  - Gentle guidance toward the next target, not aggressive (#31–32).
  - Pull back in multi-enemy combat (#33).
  - Don't pull focus while the player inspects (#35).
  - Always allow manual control (#36).
  - Lag and inertia with a cap on screen drift (#40).
  - Motion-sickness items: low FOV, dynamic FOV, shake, bob, abrupt stops at limits (#41–47).
  - Playtest with a wide range of players (#49).
  - Treat the rules as a foundation, not dogma (#50).
  - Source: [Skillbox](https://skillbox.ru/media/gamedev/50-principov-vazhnyh-dlya-sozdaniya-horoshey-igrovoy-kamery/)
- **Martel:**
  - A cut is acceptable when views differ by more than 90°.
  - Keep the old input matrix while the stick is held across a cut.
  - Everything data-driven, with activation rules and priorities.
  - "Make other people test them out."
  - Source: [Martel](https://www.gameaipro.com/GameAIPro/GameAIPro_Chapter47_Tips_and_Tricks_for_a_Robust_Third-Person_Camera_System.pdf)
- **Haigh-Hutchinson:** Paul Tozour's foreword notes that game reviews often fault cameras that lag or point the wrong way. — [Game Developer, "'Real-Time Cameras' And Mark Haigh-Hutchinson"](https://www.gamedeveloper.com/programming/-real-time-cameras-and-mark-haigh-hutchinson) (via search snippet)

### Inferences
- **Mapping to this game:**
  - The 0.5 s rigid swing breaks "avoid sudden speed and direction changes" (Keren).
  - It also breaks "move closer rather than swing" (Nesky #12) when walls are involved.
  - It sweeps the movement reference frame under the player during a turn-around. With camera-relative WASD, a fast yaw sweep while the key is held bends the agent's path. This is Martel's ping-pong problem in continuous form.
  - Fix: latch the input frame while a movement key is held, or make yaw changes slow enough that players "gradually adjust" (Martel).
- **Mouse-and-keyboard:** mouse look is direct and precise. The case for auto-recentre is weaker than on a gamepad, which is why engines default recentring to off. The leash gives "camera follows where I go" without fighting the mouse.

### Gaps
- Nesky's original slides and video were not accessed. The list above is a translated third-party summary; check the wording against the [GDC Vault](https://www.gdcvault.com/) recording ("50 Game Camera Mistakes", GDC 2014) before quoting it.
- I found nothing usable from Ian Pilipski's camera talks or articles, or from the Journey camera writing (search returned nothing relevant).
- No Insomniac, Naughty Dog or Santa Monica third-person camera talk with tuning numbers was located in this pass.
