## Toio Left Hand Lab ver1.2

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
- ver1.1 時点の既定は `TapRepeat` モード
- まずはメモ帳で `WWWWAAASSSDD` のように並ぶことを確認する想定でした
- `HoldWhileTilted` モードに切り替えると、将来の Minecraft 操作に近い「押しっぱなし」実験ができる
- 外部キー送出ログを Console で確認できます

### ver1.2 記録

- 記録日: 2026-03-20
- バージョン名: `ver1.2`
- 追加内容: Minecraft 向けの前景ウィンドウタイトル指定を追加
- 調整内容: 外部キー送出をスキャンコード優先に変更し、`A/D` の向きを修正
- 確認結果: Minecraft で `W/A/S/D` 操作が反映されることを確認

### ver1.2 仕様

- `Output Mode` の既定を `HoldWhileTilted` に変更
- `Require Foreground Window Title Match` を追加
- `Required Foreground Window Title Fragment = Minecraft` を既定値に設定
- `Minecraft` を含む前景ウィンドウにだけ `W/A/S/D` を送出
- `A/D` の左右判定は現在の実機向きに合わせて補正済み

### Minecraft 向け設定

- `WindowsExternalWasdOutput` は、前景ウィンドウのタイトルに指定文字列が含まれるときだけ送出できるようにしてあります。
- 既定シーンでは `Output Mode = HoldWhileTilted`、`Require Foreground Window Title Match = On`、`Required Foreground Window Title Fragment = Minecraft` です。
- たとえば `Minecraft 1.21.11 - シングルプレイ` のようなタイトルでも、`Minecraft` 部分一致で反応します。
- Minecraft 以外へ誤送信したくない場合は、このタイトル絞り込みを維持してください。

### 開き方

1. Unity Hub で `toio-sdk-unity` フォルダを開きます。
2. `Assets/Experiments/ToioLeftHandLab/ToioLeftHandLab.unity` を開きます。
3. 再生して `Connect` を押します。
4. Minecraft で検証する場合は、Minecraft ウィンドウを前面にします。
5. タイトル断片が異なる場合は、`WindowsExternalWasdOutput` の `Required Foreground Window Title Fragment` を調整します。
6. toio を傾けると、Minecraft 側へ `W/A/S/D` が送られます。
7. メモ帳で再確認したい場合は、タイトル絞り込みをオフにするか、断片をメモ帳のタイトルに合わせます。

### シーンの見方

- 画面左側の各ラベルに `W/A/S/D` の状態が出ます。
- `Vertical Axis` が `W/S`
- `Horizontal Axis` が `A/D`
- 画面下部のテキストボックスに、検出された `W/A/S/D` が順に入力されます。
- 画面下部テキストに実験条件と現在の補足が出ます。
- 外部入力を試す場合は、Unity の Play を維持したまま対象アプリを前面にします。

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
  - 前景ウィンドウタイトルの部分一致で送出先を絞り込める
  - 送出はゲーム入力で通りやすいスキャンコード優先
  - 既定シーンでは `HoldWhileTilted`、`Minecraft` タイトル絞り込み、Console ログ出力を有効化済み

### 前提

- これはまず `Unity 内で使う仮想キー入力` を土台にしつつ、ver1.1 で Windows 外部出力、ver1.2 で Minecraft 向け調整まで進めた実験環境です。
- `x=roll`, `y=pitch`, `z=yaw` として扱っています。
- 現在の割り当ては `A/D = roll(x)`, `W/S = pitch(y)` です。
- Windows 外部キー送出は `Unity Editor` / `Windows Standalone` を前提にしています。
- 外部出力は Windows 限定です。
- まずはメモ帳などの単純なテキスト入力先で確認し、その後にゲーム入力へ進める想定です。
- 実機の床面や摩擦でしきい値調整が必要になる可能性があります。

### メモ

- `ver1.0` は Unity 内の仮想 WASD 確認版です。
- `ver1.1` はその土台の上で、Windows 外部入力まで伸ばした試作版です。
- `ver1.2` は Minecraft の前景タイトル指定、スキャンコード優先送出、`A/D` 調整まで反映した実用確認版です。
