## Toio Left Hand Lab ver1.0

toio コア キューブ 1 台を、左手用の入力ガジェットとして試すための Unity 実験環境です。

### 目的

- マットではない平面で toio を動かす
- 左右の手旋回を `A/D`
- まずは Unity 内で `WASD` 相当の仮想キー状態が作れるかを確認する
- Unity Editor 実行中に、判定されたキー文字をシーン内テキストボックスへ入力する

### ver1.0 記録

- 記録日: 2026-03-18
- バージョン名: `ver1.0`
- 対象シーン: `Assets/Experiments/ToioLeftHandLab/ToioLeftHandLab.unity`
- 対象デバイス: toio コア キューブ 1 台
- 実行前提: Unity Editor の Play Mode

### ver1.0 仕様

- 前へ傾けると `W`
- 後ろへ傾けると `S`
- 左へ傾けると `A`
- 右へ傾けると `D`
- 判定されたキー文字は、シーン下部のテキストボックスに追記表示される
- 入力判定は姿勢角ベースで行う
- `W/S` は `pitch(y)`
- `A/D` は `roll(x)`

### ver1.0 到達点

- `W/S/A/D` を toio 1 台の傾き入力として Unity 内で扱える
- 判定結果をシーン内テキストボックスへ可視化できる
- 左手入力ガジェット実験の最初の土台として使える状態

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

### メモ

- 今回の `ver1.0` は「toio を左手レバーのように傾けて、Unity 内で `W/A/S/D` 相当の入力として扱う」最初の確認版です。
