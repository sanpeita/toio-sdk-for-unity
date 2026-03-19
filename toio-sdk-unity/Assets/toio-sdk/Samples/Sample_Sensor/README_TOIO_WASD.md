## Toio WASD Bridge

`Assets/toio-sdk/Samples/Sample_Sensor` を土台に、toio コア キューブ 1 台を `W/A/S/D` 相当の入力源として扱うための追加スクリプトです。

### この構成を選ぶ理由

- `Sample_Sensor` は 1 台接続の土台がすでにあります。
- `attitudeCallback` を使って、姿勢角ベースの入力実験を組み込みやすいです。
- 実機でも Simulator でも同じサンプル構成を流用しやすいです。

### 追加したファイル

- `ToioWasdInput.cs`
  - 前後の傾きを `attitudeCallback` の `pitch(y)` から `W/S` に変換します。
  - 左右の傾きを `attitudeCallback` の `roll(x)` から `A/D` に変換します。
  - `GetVirtualKey(KeyCode)`、`Horizontal`、`Vertical` を公開します。
  - 傾きが続く間は `W/A/S/D` の状態も継続します。
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

`W/A/S/D` の押下状態をそのまま使いたい場合は、次のようにも扱えます。

```csharp
bool moveForward = toioInput.WPressed;
bool moveLeft = toioInput.APressed;
```

### 今日の実験につながる使い方

- `ToioWasdInput` は Unity 内の仮想 WASD だけでなく、外部アプリへキー送出する実験の入力源としても使えます。
- `Assets/Experiments/ToioLeftHandLab/WindowsExternalWasdOutput.cs` と組み合わせると、Windows 上のメモ帳などへ `W/A/S/D` を送れます。
- `TapRepeat` なら `WWWWAAASSD` のような連続文字入力向けです。
- `HoldWhileTilted` なら Minecraft のような「押しっぱなし移動」検証につなげやすいです。

### 調整ポイント

- `Forward Backward Tilt Threshold Deg`
  - `W/S` とみなす `pitch(y)` のしきい値です。
- `Left Right Tilt Threshold Deg`
  - `A/D` とみなす `roll(x)` のしきい値です。
- `Invert Forward Backward`
  - 前後が逆ならオンにします。
- `Invert Turn Direction`
  - A / D が逆ならオンにします。
- `Hold Seconds`
  - 一時的な仮想キー注入の保持時間です。
- `Use Keyboard Fallback`
  - 物理キーボードの `W/A/S/D` も併用したいときに使います。

### 前提と制約

- 現在の WASD 判定は姿勢角ベースです。
- `W/S` は `pitch(y)`、`A/D` は `roll(x)` を使います。
- 傾きがしきい値未満に戻ると、対応する入力状態はオフになります。
- 実機の置き方や手のクセでしきい値調整が必要になる場合があります。
