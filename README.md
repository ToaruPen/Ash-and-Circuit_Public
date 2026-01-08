# Ash-n-Circuit（公開版）

Ash-n-Circuit は 2D タイルベースのトップダウン・ターン制ローグライクです。
世界は少数の普遍的なルールで動作する、というのが核の発想です。
火/水/油などの相互作用によって、力押しではない解決を作れます。
近接/遠距離/環境利用などのプレイスタイルで世界との関わり方が変わります。
狙いは発見と再利用です。一度学んだルールを別の状況に適用します。

## 役割別の確認場所

プログラマー
- `Assets/_Project/Scripts/Core` - ルールとターン制ロジック
- `Assets/_Project/Scripts/UnityIntegration` - 入力、描画、シーン配線
- `Assets/_Project/Scripts/UnityIntegration/UI` - HUD、ログ、インベントリ、コンテキストメニュー
- `Assets/_Project/Scenes` - `Boot`, `Discovery`, `Game`

ドット絵
- `Assets/_Project/Art/Tiles` - 地形、壁、樹木
- `Assets/_Project/Art/Overlay` - 火、水、油、プロップ、エフェクト
- `Assets/_Project/Art/Characters` - プレイヤーと敵
- `Assets/_Project/Art/Items` - アイテムアイコン
- 注: 現在のスプライトは草案のプレースホルダーです。

ゲームプランナー / デザイナー
- `docs/memo/mvp_discovery_mvp_plan.md` - 企画と MVP スコープ
- `Assets/_Project/Resources/Content` - アイテム
- `Assets/_Project/Resources/Actors` - 敵/NPC 定義
- `Assets/_Project/Resources/Props` - プロップとコンテナ
- `Assets/_Project/Resources/MessageCatalog` - ログや UI テキスト
- `Assets/_Project/Resources/Dialogs` - チュートリアル/会話テキスト

## Unity Editor 簡易確認

1. Unity Hub でこのフォルダを開く。
2. Unity `6000.3.2f1` を使う（`ProjectSettings/ProjectVersion.txt` を参照）。
3. `Assets/_Project/Scenes/Boot.unity`（または `Discovery.unity`）を開く。
4. Play してエラーなく動作することを確認する。
5. プレイヤーを移動し、タイル上で右クリックのコンテキストメニューを試す。
