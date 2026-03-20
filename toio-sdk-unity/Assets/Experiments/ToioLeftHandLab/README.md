## Toio Left Hand Lab ver1.1

toio コア キューブ 1 台を、左手用の入力ガジェットとして試すための Unity 実験環境です。

### ハッシュタグ

- `toio左手ガジェット化計画` の共有タグ: `#左手運用試験区画` `#ToioJetHand`

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

### ver1.1 記録

- 記録日: 2026-03-19
- バージョン名: `ver1.1`
- 追加内容: Windows 向けの外部キー送出実験を追加
- 確認結果: Unity Editor 実行中にメモ帳へ `W/A/S/D` を外部入力できることを確認

### ver1.1 仕様

- `WindowsExternalWasdOutput` を同シーンに追加
- Play 中に Unity 以外のウィンドウを前面にすると、`W/A/S/D` を外部アプリへ送出できる
- 既定では `TapRepeat` モード
- まずはメモ帳で `WWWWAAASSSDD` のように並ぶことを確認する想定
- `HoldWhileTilted` モードに切り替えると、将来の Minecraft 操作に近い「押しっぱなし」実験ができる
- 既定シーンでは外部キー送出ログを Console で確認できる

### 開き方

1. Unity Hub で `toio-sdk-unity` フォルダを開きます。
2. `Assets/Experiments/ToioLeftHandLab/ToioLeftHandLab.unity` を開きます。
3. 再生して `Connect` を押します。
4. メモ帳を開いて前面に出します。
5. toio を傾けると、メモ帳へ `W/A/S/D` が入力されます。
6. Minecraft 方向の検証をする場合は、`WindowsExternalWasdOutput` の `Output Mode` を `HoldWhileTilted` に切り替えます。

### シーンの見方

- 画面左側の各ラベルに `W/A/S/D` の状態が出ます。
- `Vertical Axis` が `W/S`
- `Horizontal Axis` が `A/D`
- 画面下部のテキストボックスに、検出された `W/A/S/D` が順に入力されます。
- 画面下部テキストに実験条件と現在の補足が出ます。
- 外部入力を試す場合は、Unity の Play を維持したままメモ帳を前面にします。

### 実装メモ

- `ToioWasdInput`
  - `W/S` は `attitudeCallback` の pitch
  - `A/D` は `attitudeCallback` の roll
- `ToioLeftHandLabController`
  - 既存の `Sample_Sensor` UI を流用して状態を表示
  - `W/A/S/D` 判定を受けてシーン内の InputField に文字を追記
- `WindowsExternalWasdOutput`
  - Windows の前景ウィンドウへ `W/A/S/D` を送る実験用コンポーネント
  - `TapRepeat` と `HoldWhileTilted` を切り替え可能
  - 既定シーンでは `TapRepeat` と Console ログ出力を有効化済み

### 前提

- これはまず `Unity 内で使う仮想キー入力` を土台にしつつ、ver1.1 では Windows 外部出力も試せる実験環境です。
- `x=roll`, `y=pitch`, `z=yaw` として扱っています。
- 現在の割り当ては `A/D = roll(x)`, `W/S = pitch(y)` です。
- Windows 外部キー送出は `Unity Editor` / `Windows Standalone` を前提にしています。
- 外部出力は Windows 限定です。
- まずはメモ帳などの単純なテキスト入力先で確認し、その後にゲーム入力へ進める想定です。
- 実機の床面や摩擦でしきい値調整が必要になる可能性があります。

### メモ

- `ver1.0` は Unity 内の仮想 WASD 確認版です。
- `ver1.1` はその土台の上で、Windows 外部入力まで伸ばした試作版です。
- `ver1.1` の次段では、`HoldWhileTilted` を中心に Minecraft 向けの押しっぱなし調整を行う想定です。
