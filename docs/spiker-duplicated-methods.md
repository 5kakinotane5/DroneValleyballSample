# Spiker ディレクトリの重複メソッド一覧

`Assets/Script/Spiker/` 配下の 2 ファイルに存在する、**処理内容が完全に同一のメソッド**を記録したもの。

- [SpikeDrone.cs](../Assets/Script/Spiker/SpikeDrone.cs) — プレイヤー操作のスパイカードローン（約 840 行）
- [EnemySpikeDrone.cs](../Assets/Script/Spiker/EnemySpikeDrone.cs) — Enemy AI 専用スパイカードローン（約 665 行）

EnemySpikeDrone.cs は冒頭コメントのとおり「SpikeDrone.cs の AI モード（`isPlayerControlled=false`）を独立させたもの」であり、共通基盤がコピーされたまま並存している。

以下の表に挙げたメソッドは、空白と記法（ブロック `{}` 形式 vs 式形式 `=>`）の差を除き、処理が一致する。

## 一覧

| メソッド | SpikeDrone.cs | EnemySpikeDrone.cs | 備考 |
|---|---|---|---|
| `UpdateEstimatedBallVelocity` | [297-319](../Assets/Script/Spiker/SpikeDrone.cs#L297-L319) | [217-239](../Assets/Script/Spiker/EnemySpikeDrone.cs#L217-L239) | 完全一致 |
| `TryGetTrajectoryAvoidVector` | [553-578](../Assets/Script/Spiker/SpikeDrone.cs#L553-L578) | [525-546](../Assets/Script/Spiker/EnemySpikeDrone.cs#L525-L546) | SpikeDrone 側にのみ `Ball.Exists()` の早期リターンあり（実質同等） |
| `PredictPosition` | [580-587](../Assets/Script/Spiker/SpikeDrone.cs#L580-L587) | [548-551](../Assets/Script/Spiker/EnemySpikeDrone.cs#L548-L551) | 記法のみ差異 |
| `PredictBallPosition` | [589-597](../Assets/Script/Spiker/SpikeDrone.cs#L589-L597) | [553-561](../Assets/Script/Spiker/EnemySpikeDrone.cs#L553-L561) | 完全一致 |
| `TryGetClosestApproachNormal` | [602-634](../Assets/Script/Spiker/SpikeDrone.cs#L602-L634) | [566-591](../Assets/Script/Spiker/EnemySpikeDrone.cs#L566-L591) | 完全一致（コメントも同文） |
| `SetNonTargetBallIgnore` | [636-647](../Assets/Script/Spiker/SpikeDrone.cs#L636-L647) | [593-603](../Assets/Script/Spiker/EnemySpikeDrone.cs#L593-L603) | 記法のみ差異 |
| `ApplyDodgeVelocity` | [650-677](../Assets/Script/Spiker/SpikeDrone.cs#L650-L677) | [606-629](../Assets/Script/Spiker/EnemySpikeDrone.cs#L606-L629) | 記法のみ差異 |
| `IsBallOnMySide` | [679-682](../Assets/Script/Spiker/SpikeDrone.cs#L679-L682) | [631-632](../Assets/Script/Spiker/EnemySpikeDrone.cs#L631-L632) | 記法のみ差異 |
| `CalculateFalling` | [684-705](../Assets/Script/Spiker/SpikeDrone.cs#L684-L705) | [634-650](../Assets/Script/Spiker/EnemySpikeDrone.cs#L634-L650) | 記法のみ差異 |
| `MoveToPoint` | [707-713](../Assets/Script/Spiker/SpikeDrone.cs#L707-L713) | [652-657](../Assets/Script/Spiker/EnemySpikeDrone.cs#L652-L657) | 記法のみ差異 |
| `Hover` | [715-725](../Assets/Script/Spiker/SpikeDrone.cs#L715-L725) | [659-664](../Assets/Script/Spiker/EnemySpikeDrone.cs#L659-L664) | 記法のみ差異 |
| `DetectTossType` | [729-752](../Assets/Script/Spiker/SpikeDrone.cs#L729-L752) | [325-345](../Assets/Script/Spiker/EnemySpikeDrone.cs#L325-L345) | 記法のみ差異 |
| `ResetToInitialState` | [345-355](../Assets/Script/Spiker/SpikeDrone.cs#L345-L355) | [204-214](../Assets/Script/Spiker/EnemySpikeDrone.cs#L204-L214) | 完全一致 |
| `CurrentMaxVelocity`（プロパティ） | [97-111](../Assets/Script/Spiker/SpikeDrone.cs#L97-L111) | [71-85](../Assets/Script/Spiker/EnemySpikeDrone.cs#L71-L85) | ローカル変数名のみ差異（`staminaMult` / `mult`） |

## 各メソッドが行っている処理

### ボールの状態把握

**`UpdateEstimatedBallVelocity`**
ボール速度を `Rigidbody.linearVelocity` から直接読まず、毎 FixedUpdate でボール座標を記録し、前フレームとの差分を `Time.fixedDeltaTime` で割って推定する。ボール不在時や `BallGetter.GetPosition()` が null のときは推定値をゼロにし、履歴フラグ `hasLastBallPos` を折って次に取得できたフレームを 1 フレーム目として扱い直す。

**`PredictPosition` / `PredictBallPosition`**
等加速度運動の式 `p + v·t + ½g·t²`（重力は y のみ）で t 秒後の位置を求める。`PredictBallPosition` は現在のボール位置と推定速度を差し込むラッパー。

**`CalculateFalling(h)`**
ボールが高さ `h`（＝打点 `spikeHeight`）に達するまでの時間を、二次方程式 `½g·t² + vy₀·t + (y₀ - h) = 0` を解いて求める。判別式が負（届かない）なら -1、上昇時・下降時の 2 解のうち大きい方（落ちてくる側）を返す。戻り値が `timeUntilImpact` になる。

**`DetectTossType`**
トスの頂点高さを `y + vy²/(2|g|)`（上昇中の場合。下降中は現在の y）で予測し、`highTossApexThreshold`（13m）/ `medTossApexThreshold`（8m）と比較して `TossQuality` を High / Medium / Low に分類する。この品質が打球速度の上限を決める。

**`IsBallOnMySide`**
ボールがネット（`netX`）のどちら側にあるかで自陣かを判定する。Ally は x が正側、Enemy は負側。

### 回避

**`TryGetClosestApproachNormal`**
ボール軌道を duration 秒先まで `trajectorySamples`（30）回サンプリングし、ドローンと最も近づく点とその距離を探す。最接近点でのボール→ドローン方向は軌道の接線にほぼ垂直になる（距離最小の点では距離ベクトルと速度が直交する）ため、これを「軌道から逃げる法線方向」として返す。ドローンが軌道上にほぼ乗っていて方向が定まらない場合はボール速度と上方向の外積で横に逃がし、それも縮退するなら `Vector3.forward` にフォールバックする。

**`TryGetTrajectoryAvoidVector`**
上記を「ターゲットにしているボール」に対して使い、回避速度ベクトルを作る。最接近距離が `trajectoryCheckRadius`（3m）以上なら回避不要で false。近ければ `(1 - 距離/半径)` を強度として `trajectoryAvoidSpeed`（25）を掛けた法線方向のベクトルを返し、呼び出し側が `rb.linearVelocity` に加算する。`targetRb` 確定前はコート上の任意のボールが対象になりうるため自陣判定を挟み、確定後は捕捉済みなので省く、という条件分岐まで同一。

**`ApplyDodgeVelocity`**
ターゲット**以外**のボールを避ける。`dodgeRadius`（3m）内を `Physics.OverlapSphere` で走査し、ボールタグを持ちターゲットでないものそれぞれについて最接近法線を求め、距離が近いほど重い重みで回避ベクトルを合成する。最後に速度へ加算し `vMaxDrone` でクランプ。`TryGetTrajectoryAvoidVector` が「打ちに行く球の軌道を邪魔しない」ためなのに対し、こちらは「無関係な球にぶつからない」ため。

**`SetNonTargetBallIgnore`**
ターゲット以外のボールとの物理衝突を `Physics.IgnoreCollision` で有効／無効にする。Striking 中（突進中）と Waiting 中は無関係な球を貫通させ、それ以外では通常どおり当たる。避けきれなかった球に弾かれてスパイクが破綻するのを防ぐ保険。

### 移動

**`MoveToPoint`**
目標との差分を 0.8 で割った速度を設定する（0.8 秒で到達するペース。毎フレーム再計算されるため指数的に減速しながら近づく）。上限は `vMax`（= `vMaxDrone × tossBoost`）。

**`Hover`**
目標付近に留まる移動。差分が 0.3m 未満なら速度をゼロにして座標をスナップし、それ以上なら `vMaxDrone / 8` の低速で向かう。`MoveToPoint` より遅く、待機・帰還に使う。

### その他

**`CurrentMaxVelocity`**
トス品質から基準速度を決め（High は `vMaxDrone` フル、Medium は `medVelocityRatio`=0.65 倍、Low は `weakVelocityRatio`=0.35 倍）、`StaminaSystem.SpeedMultiplier` を掛けた値を返す。「今このトスに対して出せる最大打球速度」。

**`ResetToInitialState`**
ラリー終了時などに外部から呼ばれ、状態を Waiting に戻し、ターゲット参照をすべて null にし、衝突無視を解除し、初期位置へワープさせて速度をゼロにする。

## メソッド以外の重複

上表はメソッドのみだが、次も重複している（参考）。

- **フィールド宣言** — `tossBoost` / `ballTag` / `spikeHeight` / `vMaxDrone` / `vMax` / `spikeFlightTime` / `runupTime` / `netX` / `netHeightSafe`、軌道回避 3 種、dodge 3 種、トス閾値 2 種、速度比率 2 種、着弾範囲 3 種、内部状態（`rb` / `targetRb` / `targetBall` / `requiredDroneVel` / `pointA` / `standbyPoint` / `timeUntilImpact` / `lastSpikedBall` / `g` / `pendingCourse` / `pendingVelocity` / `tossQuality` / 推定速度 3 種）
- **`enum State`** — 同じ 5 値（Waiting / Hovering / MovingToTrajectory / Striking / Returning）が別々に定義されている
  ([SpikeDrone.cs:151](../Assets/Script/Spiker/SpikeDrone.cs#L151) / [EnemySpikeDrone.cs:108](../Assets/Script/Spiker/EnemySpikeDrone.cs#L108))
- **`FixedUpdate` の状態機械** — Waiting / Hovering / Striking / Returning の 4 状態は同一処理。`MovingToTrajectory` のみ差異
  ([SpikeDrone.cs:170-294](../Assets/Script/Spiker/SpikeDrone.cs#L170-L294) / [EnemySpikeDrone.cs:128-202](../Assets/Script/Spiker/EnemySpikeDrone.cs#L128-L202))
- **`CalculateTrajectory`** — 約 90 行の弾道計算が同一の式。差は Enemy 側が pointB を `ClampLandingToCourt()` に通す 1 点のみ
  ([SpikeDrone.cs:428-517](../Assets/Script/Spiker/SpikeDrone.cs#L428-L517) / [EnemySpikeDrone.cs:347-430](../Assets/Script/Spiker/EnemySpikeDrone.cs#L347-L430))
- **打球速度の計算** — SpikeDrone は `CalcBallVelocity()` に切り出し済みだが、EnemySpikeDrone は同じ計算を `OnCollisionEnter` にインライン展開している。差は `speedMult`（タイミングボーナス）と `ClampLandingToCourt` の有無のみ
  ([SpikeDrone.cs:756-801](../Assets/Script/Spiker/SpikeDrone.cs#L756-L801) / [EnemySpikeDrone.cs:447-491](../Assets/Script/Spiker/EnemySpikeDrone.cs#L447-L491))
- **`OnCollisionEnter` の前段** — ガード条件 4 つ、`lastTeamToHit` 設定、`RecoveryBlocked = false` まで同一。スタミナ消費が `ConsumeChargeWithTiming` か `ConsumeCharge` かのみ差異
  ([SpikeDrone.cs:520-537](../Assets/Script/Spiker/SpikeDrone.cs#L520-L537) / [EnemySpikeDrone.cs:432-445](../Assets/Script/Spiker/EnemySpikeDrone.cs#L432-L445))
- **着弾点の算出式** — `norm` → `targetX` の Lerp と `blur` の適用が計 4 箇所にコピーされている
  ([SpikeDrone.cs:435-443](../Assets/Script/Spiker/SpikeDrone.cs#L435-L443) / [SpikeDrone.cs:758-766](../Assets/Script/Spiker/SpikeDrone.cs#L758-L766) / [EnemySpikeDrone.cs:353-362](../Assets/Script/Spiker/EnemySpikeDrone.cs#L353-L362) / [EnemySpikeDrone.cs:448-457](../Assets/Script/Spiker/EnemySpikeDrone.cs#L448-L457))
- **トス品質 → 速度上限のマッピング** — `CurrentMaxVelocity` のほか、SpikeDrone の `FindAndCalculateBall` の Enemy 分岐にも手書きされている
  ([SpikeDrone.cs:399-411](../Assets/Script/Spiker/SpikeDrone.cs#L399-L411))
- **死にコード** — SpikeDrone の `MovingToTrajectory` にある `isPlayerControlled == false` 側の分岐は、EnemySpikeDrone の同状態と同一処理。EnemySpikeDrone が独立した現在、到達しない前提のはず
  ([SpikeDrone.cs:251-264](../Assets/Script/Spiker/SpikeDrone.cs#L251-L264))

## 本質的な差分

コピーの正体は「ボールの物理予測」「回避」「弾道計算」というチーム差と無関係な共通基盤が丸ごと複製されていること。実際に異なるのは次の 3 点のみ。

1. **打球パラメータを誰がどう決めるか** — プレイヤー入力（`inputCourse` / `inputVelocity`、K を離した瞬間に確定）vs AI 配球（`ChooseStrategy()` が Ally の位置を読んで逆サイド・逆深度を狙う）
2. **着弾点をコート内にクランプするか** — Enemy のみ `ClampLandingToCourt()` を適用
3. **タイミングボーナスを乗せるか** — SpikeDrone のみ `TimingWindowSystem` と `ConsumeChargeWithTiming` を使う
