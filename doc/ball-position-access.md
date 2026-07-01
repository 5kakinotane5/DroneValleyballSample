# ボール位置取得箇所の一覧

対象ファイル（現在編集中のシーンにアタッチされている3ファイル）について、
`Ball` クラスの使用有無に関わらず、ボールの位置を取得している箇所を全て挙げる。

## SpikeDrone.cs

### Ball クラス経由 (`Ball.GetPosition()`)
- L331, L334（FindAndCalculateBall の判定）
- L400（CalcBallVelocity 内）
- L607（着地予測）
- L649（影位置計算）

### Ball クラス非経由
- L491 `collision.transform.position` — スパイク衝突時のボール位置
- L553-558 `PredictBallPosition()` 内で `ballRb.position` を起点に未来位置を予測（呼び出し元: L207, L228, L523）
- L509 `anyBall.transform.position` — コートサイド判定用（DodgeBall 内）
- L584 `col.transform.position` — 回避対象ボールの位置

## EnemySpikeDrone.cs

### Ball クラス経由 (`Ball.GetPosition()`)
- L210, L213（FindAndCalculateBall の判定）
- L283（着地予測）
- L313（CalcBallVelocity 内）
- L548（影/着地計算）

### Ball クラス非経由
- L404 `collision.transform.position` — スパイク衝突時の hitPos
- L507-510 `PredictBallPosition()` 内で `ballRb.position` を起点に予測（呼び出し元: L486）
- L473 `anyBall.transform.position` — コートサイド判定用
- L530 `col.transform.position` — 回避対象ボールの位置

## newReceiverAllyEnemy.cs

### Ball クラス経由 (`Ball.GetPosition()`)
- L148（着地点予測）
- L166（FindAndCalculateBall）
- L183（IsBallGoingOut のアウト判定）

### Ball クラス非経由
- L210 `collision.transform.position` — レシーブ衝突時の startPos

## 補足
- `PredictBallPosition()`（Spiker 2ファイル）と `PredictLandingPoint()`（Receiver）は
  引数で受け取った位置から未来位置を計算する関数。
  Receiver の `PredictLandingPoint` は内部でボールを直接参照せず、
  呼び出し時に渡される `Ball.GetPosition()` の値（L148, L166, L183）が位置取得の起点。
- `GameObject.FindGameObjectWithTag(ballTag)` でボール自体は取得しているが、
  そこから位置を読むのは上記の `Ball.GetPosition()` や `transform.position` 箇所。
