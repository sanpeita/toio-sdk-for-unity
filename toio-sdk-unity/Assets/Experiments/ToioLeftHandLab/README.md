## Toio Left Hand Lab ver1.0

toio コア キューブ 1 台を、左手用の入力ガジェットとして試すための Unity 実験環境です。

### 目的

- マットではない平面で toio を動かす
- 左右の手旋回を `A/D`
- まずは Unity 内で `WASD` 相当の仮想キー状態が作れるかを確認する
- Unity Editor 実行中に、判定されたキー文字をシーン内テキストボックスへ入力する

### 今日の到達点

- `A/D` の旋回検出は実用的に動作
- `W/S` は仕様変更し、前後への傾き入力として扱う

### 開き方

1. Unity Hub で `toio-sdk-unity` フォルダを開きます。
2. `Assets/Experiments/ToioLeftHandLab/ToioLeftHandLab.unity` を開きます。
3. 再生して `Connect` を押します。

### シーンの見方

- 画面左側の各ラベルに `W/A/S/D` の状態が出ます。
- `Vertical Axis` が `W/S`
- `Horizontal Axis` が `A/D`
- 画面下部のテキストボックスに、検出された `W/A/S/D` が順に入力されます。
- 画面下部テキストに実験条件と現在の補足が出ます。

### 実装メモ

- `ToioWasdInput`
  - `W/S` は `attitudeCallback` の pitch
  - `A/D` は `attitudeCallback` の roll
- `ToioLeftHandLabController`
  - 既存の `Sample_Sensor` UI を流用して状態を表示
  - `W/A/S/D` 判定を受けてシーン内の InputField に文字を追記

### 前提

- これはまず `Unity 内で使う仮想キー入力` の実験環境です。
- `x=roll`, `y=pitch`, `z=yaw` として扱っています。
- 現在の割り当ては `A/D = roll(x)`, `W/S = pitch(y)` です。
- OS 全体に対して物理キーボードのようにキー送出する機能は、現時点では入れていません。
- 実機の床面や摩擦でしきい値調整が必要になる可能性があります。
