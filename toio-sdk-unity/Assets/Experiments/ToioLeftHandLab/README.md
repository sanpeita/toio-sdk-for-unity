## Toio Left Hand Lab ver1.3

toio コア キューブ 1 台または 2 台を、左手用の入力ガジェットとして試すための Unity 実験環境です。

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

### ver1.3 記録

- 初回記録日: 2026-03-20
- 更新日: 2026-03-21
- バージョン名: `ver1.3`
- 状況: 2 台接続の `twin stick mode` を実装し、Minecraft 試験前の Unity 内確認まで到達
- 方針: 1 台入力系を土台に、2 台のコアキューブの傾き組み合わせで追加キーを扱う段階へ進める
- 備考: 2026-03-21 時点で Unity 内の確認項目は `W/A/S/D` `LeftShift` `Space` `LeftCtrl`

### ver1.3 仕様

- `ver1.2` の 1 台入力系を土台に、2 台接続の `twin stick mode` を追加
- `1stick mode` は従来どおり 1 台の傾きで `W/A/S/D`
- `twin stick mode` は 2 台のコアキューブを同時接続して入力を扱う
- `W/S` は 2 台の共有 pitch 判定
- `A/D` は 2 台の共有 roll 判定
- 2 台の内向き傾きで `LeftShift`
- 2 台の外向き傾きで `Space`
- どちらかのキューブのボタン押下で `LeftCtrl`
- twin 接続は `CubeManager` ベースの再試行付きフローを採用
- twin の `Cube1/Cube2` 表示はスキャン順ではなく BLE address 順で安定化
- 画面中央の水色ウィンドウは 2 列構成へ変更
- 画面下部のキー確認テキストボックスは右へ寄せて見切れにくく調整

### Minecraft 向け設定

- `WindowsExternalWasdOutput` は、前景ウィンドウのタイトルに指定文字列が含まれるときだけ送出できるようにしてあります。
- 既定シーンでは `Output Mode = HoldWhileTilted`、`Require Foreground Window Title Match = On`、`Required Foreground Window Title Fragment = Minecraft` です。
- たとえば `Minecraft 1.21.11 - シングルプレイ` のようなタイトルでも、`Minecraft` 部分一致で反応します。
- Minecraft 以外へ誤送信したくない場合は、このタイトル絞り込みを維持してください。
- `ver1.3` では `W/A/S/D` に加えて `LeftShift` `Space` `LeftCtrl` も対象にしています。

### 開き方

1. Unity Hub で `toio-sdk-unity` フォルダを開きます。
2. `Assets/Experiments/ToioLeftHandLab/ToioLeftHandLab.unity` を開きます。
3. 再生して、必要なモードを選んで `Connect` を押します。
4. `twin stick mode` を試す場合は、2 台のコアキューブを近くに置いて接続します。
5. Unity 内で `W/A/S/D` `LeftShift` `Space` `LeftCtrl` の表示を確認します。
6. Minecraft で検証する場合は、Minecraft ウィンドウを前面にします。
7. タイトル断片が異なる場合は、`WindowsExternalWasdOutput` の `Required Foreground Window Title Fragment` を調整します。

### シーンの見方

- 画面左側の各ラベルに、選択中モードに応じたキー状態が出ます。
- 水色ウィンドウは 2 列構成で、左に接続状態、右にキューブ詳細や補足を表示します。
- `1stick mode` では `Vertical Axis = W/S`, `Horizontal Axis = A/D`
- `twin stick mode` では `W/A/S/D` に加えて `Shift(inner)` `Space(outer)` `Ctrl` が出ます。
- 画面下部のテキストボックスに、検出されたキーが順に入力されます。
- 画面下部テキストに実験条件と現在の補足が出ます。
- 外部入力を試す場合は、Unity の Play を維持したまま対象アプリを前面にします。

### ver1.3 試行錯誤ログ

- 2026-03-20: `ver1.3` を開始。まずは 2 台接続と `twin stick mode` の土台を作る方針を整理。
- 2026-03-20: 既存の 1 台入力を残したまま、2 台接続コードを追加し始めた。
- 2026-03-21: 公式サンプルの複数接続フローを見直し、`CubeManager` ベースの再試行付き接続へ変更。
- 2026-03-21: 水色ウィンドウを 1 列から 2 列へ変更し、読みやすさを優先して右寄せ調整を実施。
- 2026-03-21: 下段のキー確認テキストボックスも右へ寄せて、16:9 画面での見切れを軽減。
- 2026-03-21: twin の内向き/外向き傾きで `LeftShift` / `Space` を追加。
- 2026-03-21: いったん `LeftShift` / `Space` の内外対応が逆になったが、Unity 内確認で反応自体は取れていたため、最終的に入れ替えて正しい向きへ戻した。
- 2026-03-21: 以前の `A/D` は実機向きに合わせて反転調整したが、今回の twin 追加では `Shift` / `Space` 側も実機確認に合わせて正位へ戻した。

### 実装メモ

- `ToioWasdInput`
  - `W/S` は `attitudeCallback` の pitch
  - `A/D` は `attitudeCallback` の roll
- `ToioLeftHandLabController`
  - 既存の `Sample_Sensor` UI を土台にしつつ、`ver1.3` では twin 入力へ拡張
  - `CubeManager` ベースで 2 台接続を扱う
  - twin では `W/A/S/D` `LeftShift` `Space` `LeftCtrl` を統合表示
- `WindowsExternalWasdOutput`
  - Windows の前景ウィンドウへキーを送る実験用コンポーネント
  - `TapRepeat` と `HoldWhileTilted` を切り替え可能
  - 前景ウィンドウタイトルの部分一致で送出先を絞り込める
  - 送出はゲーム入力で通りやすいスキャンコード優先
  - 既定シーンでは `HoldWhileTilted`、`Minecraft` タイトル絞り込み、Console ログ出力を有効化済み

### 前提

- これはまず `Unity 内で使う仮想キー入力` を土台にしつつ、ver1.1 で Windows 外部出力、ver1.2 で Minecraft 向け調整、ver1.3 で次段階の実験に着手した実験環境です。
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
- `ver1.3` は 2 台接続や新モードを含む次段階の実験に着手した版です。
