## toioBlenderLab ver1.1.1

`toio左手ガジェット化計画 / ToioJetHand` のうち、toio コア キューブ 2 台で Blender 用の左手入力ガジェット化を試す Unity 実験環境です。

### 名称整理

- 大元プロジェクト名: `toio左手ガジェット化計画` / `ToioJetHand`
- Minecraft をツインスティック風に試す企画名: `toioLeftHandLab`
- Blender を扱う左手ガジェット化企画名: `toioBlenderLab`

### ハッシュタグ

- `toio左手ガジェット化計画` の共有タグ: `#左手運用試験区画` `#ToioJetHand`

### 目的

- Blender 用に、toio コア キューブ 2 台で扱える最小操作セットを作る
- `Cube 1` にビュー操作、`Cube 2` に見せ場の強い単発編集アクションを分ける
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

### ver1.1 記録

- 記録日: 2026-03-22
- バージョン名: `ver1.1`
- 対象シーン: `Assets/Experiments/ToioBlenderLab/ToioBlenderLab.unity`
- 対象デバイス: toio コア キューブ 2 台
- 実行前提: `Unity Editor` または `Windows Standalone`

### ver1.1 仕様

- `Connect Cubes` で 2 台同時接続する
- `Cube 1` は `ver1.0` 継続
- `Cube 1` の左右の傾き `roll(x)` で `Orbit`
- `Cube 1` の前後の傾き `pitch(y)` で `Zoom`
- `Cube 1` のボタン押下で `Tab`
- `Cube 2` の前傾で `Plane` を選ぶ
- `Cube 2` の後傾で `Cube` を選ぶ
- `Cube 2` のボタン押下で、選択中の `Plane / Cube` を追加する
- `Cube 2` の左傾で `Solid`
- `Cube 2` の右傾で `Material Preview`
- `Cube 2` の傾きは選択または表示切替に使い、単発の追加実行はボタンに寄せる
- `Cube 1 / Cube 2` の割り当ては BLE address 順で安定化する

### ver1.1 到達点

- 左手の `Cube 1` で視点を動かしながら、`Cube 2` で Blender の編集開始アクションを入れられる
- `Cube 2` で追加対象の選択と追加実行を分離し、`Add Cube / Add Plane` を Windows 外部出力として送れる
- `Material Preview / Solid` も同じ `Cube 2` マクロの土台に乗せてある

### ver1.1.1 記録

- 記録日: 2026-03-22
- バージョン名: `ver1.1.1`
- 位置づけ: `ver1.1` をベースにした trial 調整ログ
- 目的: `Cube 2` のメッシュ追加を、実機でより破綻しにくい操作系へ寄せる

### ver1.1.1 試行概要

- `Cube 1`
  - `Orbit / Zoom / Tab` は実機で概ね成立
  - `Zoom` の前後方向は修正済み
- `Cube 2`
  - `Material Preview / Solid` の切替は成立
  - 左右の実機感覚に合わせて、現在は `左 = Solid` `右 = Material Preview`
  - `Add Plane / Add Cube` は Blender メニュー直接操作では不安定だったため、Blender 側 Python ブリッジへ移行
- Blender ブリッジ
  - `toio_blender_command_bridge.py` を追加し、`jsonl` コマンドファイル監視で `Plane / Cube` を直接生成する方式へ変更
  - Blender Text Editor への貼り付け実行、`__file__` の扱い、保存先パス、BOM 混入などを順に調整

### ver1.1.1 現在の到達点

- `Cube 1` のビュー操作は、ショートで見せられる段階まで来ている
- `Cube 2` の表示切替もショート向けの見せ場として使える
- Blender 側ブリッジ経由で `Plane / Cube` を追加する経路自体は通っている

### ver1.1.1 未解決事項

- `Cube 2` のメッシュ追加が、1 回の操作で複数回発火する
- 調整の過程では `5 個ずつ`、最終時点では `7 個ずつ` 生成される事象を確認
- 前後傾の直接追加だけでなく、`前後傾で選択 + ボタンで追加` の試作でも多重生成が残っている
- そのため、`Plane / Cube` の追加は経路成立までは確認済みだが、単発精度は未達

### ver1.1.1 ここまでに試したこと

- `Cube 2` の tilt にしきい値・中立復帰・クールダウンを追加
- `PoseType` ベース判定への切替
- `前後傾 = 直接追加` から `前後傾 = 選択 / ボタン = 追加` への試作変更
- Unity 側の重複送信抑止
- Blender 側の重複受信抑止
- Blender 側スクリプトの再実行時の監視重複対策

### ver1.1.1 判断メモ

- 現時点では、`Cube 2` の `Plane / Cube` 追加は「技術試作として経路が通った段階」
- 安定して見せやすい要素は、`Cube 1` のビュー操作と `Cube 2` の `Solid / Material Preview` 切替
- メッシュ追加は、今後も継続調整対象とする

### 開き方

1. Unity Hub で `toio-sdk-unity` フォルダを開きます。
2. `Assets/Experiments/ToioLauncher/ToioLauncher.unity` から開くか、`Assets/Experiments/ToioBlenderLab/ToioBlenderLab.unity` を直接開きます。
3. `toioBlenderLab` に入って再生し、`Connect Cubes` を押します。
4. Blender で [Text Editor] を開き、`toio-sdk-unity/BlenderBridge/toio_blender_command_bridge.py` を読み込んで `Run Script` します。
5. Blender を前面にします。
6. `Cube 1` の左右で `Orbit`、前後で `Zoom`、ボタン押下で `Tab` を確認します。
7. `Cube 2` の前後で `Plane / Cube` の選択、ボタンで追加、左で `Solid`、右で `Material Preview` を確認します。

### シーンの見方

- 上段カードに `Cube 1` と `Cube 2` の役割分担を表示します。
- 中段カードに接続状態とライブ状態を表示します。
- 下段ボタンから `ToioLauncher` と `toioLeftHandLab` へ移動できます。
- `Connection`
  - 接続メッセージを表示します。
- `Cube`
  - `Cube 1 / Cube 2` の `pose` `button` 状態を表示します。
- `Input`
  - `orbit` `zoom` の正規化値、現在アクション、`Cube 1 / Cube 2` の `euler x/y`、選択中の追加対象、キュー済みマクロ数を表示します。
- `Output`
  - Blender 前景判定や現在の外部出力状態を表示します。

### Blender 向け設定

- `WindowsExternalBlenderOutput` は、前景ウィンドウタイトルに `Blender` を含むときだけ送出します。
- Orbit は `中ボタン押下 + マウス左右移動` 相当で送ります。
- Zoom は `マウスホイール` 相当で送ります。
- Tab はキューブ側でキューされ、Blender が前景の間に送出されます。
- `Add Plane / Add Cube` は Blender 側の `toio_blender_command_bridge.py` がコマンドファイルを監視し、`mesh` を直接生成します。
- `Plane / Cube` の選択は Unity 側で持ち、実際の追加は `Cube 2` ボタン押下時だけ送出します。
- Unity 側のコマンド出力先は `toio-sdk-unity/BlenderBridge/toio_blender_bridge_commands.jsonl` です。
- これにより `Add` メニューの選択状態やカーソル位置の影響を受けずに `Plane / Cube` を置ける想定です。
- `Material Preview / Solid` は `Z` ピーメニューの英語アクセラレータ `M / S` を使う想定です。Blender の UI 言語やキーマップが異なる場合は調整が必要です。

### 実装メモ

- `ToioBlenderCubeInput`
  - 2 台接続を扱う
  - `Cube 1` の `attitudeCallback` から `roll/pitch` を読んで `Orbit / Zoom / Tab` を公開する
  - `Cube 2` の前後傾きを `Plane / Cube` の選択に使い、ボタン押下で追加を実行する
  - `Cube 2` の左右傾きは `Solid / Material Preview` の one-shot マクロへ変換する
  - `Cube 2` の tilt 判定は中立復帰まで再発火しない
- `WindowsExternalBlenderOutput`
  - Windows 前景ウィンドウへ Blender 用の入力を送る
  - `Orbit` は `MIDDLEDOWN + MOVE`
  - `Zoom` は `WHEEL`
  - `Tab` と表示切替はキュー順にキーボード送出する
  - `Plane / Cube` はコマンドファイルへ書き出して Blender 側スクリプトへ渡す
- `toio_blender_command_bridge.py`
  - Blender 側でコマンドファイルを監視する
  - `add_plane` / `add_cube` を受けて `mesh` を直接生成する
- `ToioBlenderLabController`
  - `toioBlenderLab` の UI と接続ボタン、状態表示、シーン遷移ボタンを担当する

### シーン構成

- `ToioLauncher`
  - `toio左手ガジェット化計画 / ToioJetHand` の入口シーン
  - `toioLeftHandLab` と `toioBlenderLab` へ分岐する
- `ToioBlenderLab`
  - Blender 向け 2 台入力の専用シーン
- `ToioLeftHandLab`
  - Minecraft 系の 1stick / twin stick 実験シーン

### 前提

- 現在の入力判定は姿勢角ベースです。
- `Cube 1` は `Orbit = roll(x)`、`Zoom = pitch(y)` を使います。
- `Cube 2` は前後傾きで追加対象を選び、左右傾きで表示切替、ボタンで追加を実行します。
- 外部出力は Windows 限定です。
- Blender のキーマップ変更やアドオン構成によって体感が変わる可能性があります。
- 実機の持ち方や傾き癖に応じて、しきい値や感度調整が必要になる場合があります。

### メモ

- `ver1.0` は Blender 向け最小 3 機能の初回成立版です。
- `ver1.1` は 2 台に役割分担し、`見る` から `作り始める` へ進む最小版です。
- 将来的に `Pan` や追加ショートカットを入れる場合も、まずはこの土台を育てる想定です。
- 表示名は `toio左手ガジェット化計画 / ToioJetHand`、`toioLeftHandLab`、`toioBlenderLab` に統一しています。

### ver1.1.1 ショート動画投稿文案ログ

- 記録日: `2026-03-22`
- 用途: `ToioBlenderLab ver1.1.1` 紹介ショート
- 文案方針:
  - 見せ場は `Cube 1` のビュー操作と `Cube 2` の `Solid / Material Preview` 切替
  - `Plane / Cube` 追加は「ブリッジ経由で継続調整中」と表現し、過度に完成扱いしない

#### YouTube Shorts タイトル

```text
toio 2台で Blender 左手操作を試す | ToioBlenderLab ver1.1.1 #Shorts #ToioJetHand #toio
```

#### YouTube 概要欄

```text
toio 2台で Blender を左手操作する試作、ToioBlenderLab ver1.1.1 の紹介ショートです。
Cube 1 は Orbit / Zoom / Tab、Cube 2 は Solid / Material Preview を担当。
Plane / Cube 追加は Blender ブリッジ経由で経路確認済み、単発精度は継続調整中です。

#Shorts #ToioJetHand #左手運用試験区画 #toio #Blender #Unity
```

#### X シェア文

- 想定: URL を `60` 文字前後で差し込む前提

```text
toio 2台で Blender 左手操作。ToioBlenderLab ver1.1.1 のショートです。Cube 1 は視点操作、Cube 2 は表示切替。追加機能は継続調整中。<URL> #ToioJetHand #左手運用試験区画 #toio #Blender
```

#### Facebook コミュニティ投稿文

```text
ToioBlenderLab ver1.1.1 のショートを投稿しました。
toio コア キューブ 2台で Blender の左手操作を試していて、今回は Cube 1 の Orbit / Zoom / Tab と、Cube 2 の Solid / Material Preview 切替を中心に見せています。
Plane / Cube 追加は Blender ブリッジ経由で経路確認済みで、単発精度は継続調整中です。
<URL>

#ToioJetHand #左手運用試験区画 #toio #Blender #Unity
```

#### toio Slack コミュニティ投稿文

```text
ToioBlenderLab ver1.1.1 のショートです。
toio 2台で Blender の左手操作を試していて、Cube 1 で Orbit / Zoom / Tab、Cube 2 で Solid / Material Preview 切替を担当させています。
Plane / Cube 追加は Blender ブリッジ経由で継続調整中ですが、操作の流れはかなり見えてきました。
<URL>

#ToioJetHand #左手運用試験区画 #toio #Blender
```

#### TikTok 概要欄

```text
JP:
toio 2台で Blender を左手操作する試作です。ToioBlenderLab ver1.1.1 では Cube 1 が Orbit / Zoom / Tab、Cube 2 が Solid / Material Preview を担当。Plane / Cube 追加は Blender ブリッジ経由で継続調整中です。

EN:
This is a two-toio prototype for left-hand Blender control. In ToioBlenderLab ver1.1.1, Cube 1 handles Orbit / Zoom / Tab and Cube 2 handles Solid / Material Preview. Plane / Cube adding via the Blender bridge is still being refined.

#ToioJetHand #左手運用試験区画 #toio #Blender #Unity #TikTok
```
