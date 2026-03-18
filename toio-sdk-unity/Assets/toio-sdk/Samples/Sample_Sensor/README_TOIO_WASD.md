## Toio WASD Bridge

`Assets/toio-sdk/Samples/Sample_Sensor` を土台に、toio コア キューブ 1 台を `W/A/S/D` 相当の入力源として扱うための追加スクリプトです。

### この構成を選ぶ理由

- `Sample_Sensor` は 1 台接続の土台がすでにあります。
- `motorSpeedCallback` と `attitudeCallback` の両方を使っており、今回の仕様に必要な API が最初からそろっています。
- 実機でも Simulator でも同じサンプル構成を流用しやすいです。

### 追加したファイル

- `ToioWasdInput.cs`
  - 前後の手転がしを `motorSpeedCallback` から `W/S` に変換します。
  - 左右の手旋回を `attitudeCallback` の yaw 差分から `A/D` に変換します。
  - `GetVirtualKey(KeyCode)`、`Horizontal`、`Vertical` を公開します。
- `ToioWasdDemoMover.cs`
  - Unity オブジェクトを `WASD` と同じ感覚で動かす最小デモです。

### 使い方

1. `Sample_Sensor.unity` を開きます。
2. 空の GameObject を 1 つ作り、`ToioWasdInput` を追加します。
3. 動かしたい GameObject に `ToioWasdDemoMover` を追加し、`Input Source` に `ToioWasdInput` を割り当てます。
4. 実機で使う場合は `Connect Type = Real` のまま再生します。

### 既存のキーボード処理へ組み込む場合

`Input.GetKey(KeyCode.W)` のような直接参照を、`toioInput.GetVirtualKey(KeyCode.W)` に置き換えるのが一番簡単です。

```csharp
if (toioInput.GetVirtualKey(KeyCode.W))
{
    // W キーと同じ処理
}
```

軸入力に寄せたい場合は次のように使えます。

```csharp
float horizontal = toioInput.Horizontal;
float vertical = toioInput.Vertical;
```

### 調整ポイント

- `Move Speed Threshold`
  - 前進 / 後退とみなす最低速度です。
- `Straight Diff Threshold`
  - 左右の車輪差がこの値を超えると、前後入力ではなく旋回寄りとして無視します。
- `Turn Velocity Threshold Deg Per Sec`
  - 左右旋回とみなす yaw 角速度の閾値です。
- `Invert Forward Backward`
  - 前後が逆ならオンにします。
- `Invert Turn Direction`
  - A / D が逆ならオンにします。

### 前提と制約

- マットなし平面では絶対座標は取れないため、前後判定はモーター速度、左右判定は姿勢角の変化に依存します。
- 床面が滑りやすすぎるとモーター速度が十分に出ず、前後判定が弱くなることがあります。
- 姿勢角は BLE バージョンに応じて `PreciseEulers` から自動的に利用可能な形式へ寄るため、古い実装では旋回判定がやや粗くなる場合があります。
