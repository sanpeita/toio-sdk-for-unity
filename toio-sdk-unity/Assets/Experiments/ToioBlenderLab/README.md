## toioBlenderLab ver1.0

`toio左手ガジェット化計画 / ToioJetHand` のうち、toio コア キューブ 1 台で Blender 用の左手入力ガジェット化を試す Unity 実験環境です。

### 名称整理

- 大元プロジェクト名: `toio左手ガジェット化計画` / `ToioJetHand`
- Minecraft をツインスティック風に試す企画名: `toioLeftHandLab`
- Blender を扱う左手ガジェット化企画名: `toioBlenderLab`

### ハッシュタグ

- `toio左手ガジェット化計画` の共有タグ: `#左手運用試験区画` `#ToioJetHand`

### 目的

- Blender 用に、toio コア キューブ 1 台で扱える最小操作セットを作る
- 左手の物理入力として `Orbit` `Zoom` `Tab` を成立させる
- Unity Editor 実行中に、前景の Blender ウィンドウへ操作を送る

### ver1.0 記録

- 記録日: 2026-03-21
- バージョン名: `ver1.0`
- 対象シーン: `Assets/Experiments/ToioBlenderLab/ToioBlenderLab.unity`
- 対象デバイス: toio コア キューブ 1 台
- 実行前提: `Unity Editor` または `Windows Standalone`

### ver1.0 仕様

- `Connect Cube` で 1 台接続する
- 左右の傾き `roll(x)` で Blender の `Orbit` を行う
- 前後の傾き `pitch(y)` で Blender の `Zoom` を行う
- 前へ傾けるとズームイン、後ろへ傾けるとズームアウト
- ボタン押下で `Tab` を送る
- `Tab` は中立付近で押したときに通す
- 外部出力は Blender ウィンドウが前景のときだけ有効
- Blender 判定はウィンドウタイトルの `Blender` 部分一致で行う
- Orbit 感度は初期実装から減速し、ゆっくり回せるよう調整済み

### ver1.0 到達点

- toio 1 台で Blender 向けの最小 3 機能 `Orbit / Zoom / Tab` を送れる
- シーン内に接続状態、姿勢状態、現在の入力状態、外部出力状態を表示できる
- `ToioLauncher -> toioBlenderLab` で実験系を分けたまま検証できる
- `toioBlenderLab` から `ToioLauncher` と `toioLeftHandLab` へ戻れる

### 開き方

1. Unity Hub で `toio-sdk-unity` フォルダを開きます。
2. `Assets/Experiments/ToioLauncher/ToioLauncher.unity` から開くか、`Assets/Experiments/ToioBlenderLab/ToioBlenderLab.unity` を直接開きます。
3. `toioBlenderLab` に入って再生し、`Connect Cube` を押します。
4. Blender を前面にします。
5. 左右へ傾けて `Orbit`、前後へ傾けて `Zoom`、ボタン押下で `Tab` を確認します。

### シーンの見方

- 上段カードに `Orbit / Zoom / Tab` の最小操作セットを表示します。
- 中段カードに接続状態とライブ状態を表示します。
- 下段ボタンから `ToioLauncher` と `toioLeftHandLab` へ移動できます。
- `Connection`
  - 接続メッセージを表示します。
- `Cube`
  - `pose` `button` `neutral` の状態を表示します。
- `Input`
  - `orbit` `zoom` の正規化値と現在アクション、`euler x/y` を表示します。
- `Output`
  - Blender 前景判定や現在の外部出力状態を表示します。

### Blender 向け設定

- `WindowsExternalBlenderOutput` は、前景ウィンドウタイトルに `Blender` を含むときだけ送出します。
- Orbit は `中ボタン押下 + マウス左右移動` 相当で送ります。
- Zoom は `マウスホイール` 相当で送ります。
- Tab はキューブ側でキューされ、Blender が前景の間に送出されます。

### 実装メモ

- `ToioBlenderCubeInput`
  - 1 台接続を扱う
  - `attitudeCallback` から `roll/pitch` を読む
  - `buttonCallback` と `poseCallback` を扱う
  - `Orbit / Zoom / Tab` 用の状態を公開する
- `WindowsExternalBlenderOutput`
  - Windows 前景ウィンドウへ Blender 用の入力を送る
  - `Orbit` は `MIDDLEDOWN + MOVE`
  - `Zoom` は `WHEEL`
  - `Tab` はキーボード送出
- `ToioBlenderLabController`
  - `toioBlenderLab` の UI と接続ボタン、状態表示、シーン遷移ボタンを担当する

### シーン構成

- `ToioLauncher`
  - `toio左手ガジェット化計画 / ToioJetHand` の入口シーン
  - `toioLeftHandLab` と `toioBlenderLab` へ分岐する
- `ToioBlenderLab`
  - Blender 向け 1 台入力の専用シーン
- `ToioLeftHandLab`
  - Minecraft 系の 1stick / twin stick 実験シーン

### 前提

- 現在の入力判定は姿勢角ベースです。
- `Orbit = roll(x)`、`Zoom = pitch(y)` を使います。
- 外部出力は Windows 限定です。
- Blender のキーマップ変更やアドオン構成によって体感が変わる可能性があります。
- 実機の持ち方や傾き癖に応じて、しきい値や感度調整が必要になる場合があります。

### メモ

- `ver1.0` は Blender 向け最小 3 機能の初回成立版です。
- まずは `Orbit / Zoom / Tab` を安定させることを優先しています。
- 将来的に `Pan` や追加ショートカットを入れる場合も、まずはこの 1 台入力版を土台にする想定です。
- 表示名は `toio左手ガジェット化計画 / ToioJetHand`、`toioLeftHandLab`、`toioBlenderLab` に統一しています。
