# Slap Squad

[English](README.md), [Korean](README.ko.md), [Japanese](README.ja.md)

水風船の戦闘、試合報酬、アイテムショップがつながるチーム制作のマルチプレイヤーゲームです。

| プロジェクト情報 | 内容 |
|---|---|
| 開発状況 | チーム開発 |
| 使用技術 | C++, Winsock, C#, Unity |
| 期間とチーム構成 | 2025.03 ~ 2026.02, 8名：プログラマー2名、グラフィック6名 |
| 担当 | ゲームプレイ、C++サーバーとクライアント連携 |

[開発事例を見る](https://ysl-ryudo.github.io/ja/work/bnb/), [Portfolio](https://ysl-ryudo.github.io/ja/)

[![Slap Squad](https://i.ytimg.com/vi/1LMGsY_qY_8/hqdefault.jpg)](https://www.youtube.com/watch?v=1LMGsY_qY_8)

[Slap Squad, OFY Studio](https://www.youtube.com/watch?v=1LMGsY_qY_8), [Early development demo](https://www.youtube.com/watch?v=MI5NtNU9eP8)

## ゲーム全体で担当した実装

C++サーバーに加え、Unityの7種の武器、移動・戦闘変更、マップ・水風船、試合結果、通信連携を担当しました。クライアントは別のプログラマーとの共同制作です。共同ゲームコードとShopUIの自分の変更を、他メンバーのロビー・ショップ・プロフィールUIと区別します。

**プレイの流れ**

アカウント → ルーム・キャラクター・チーム → マップと戦闘 → 脱落と勝者 → 報酬と復帰 → ショップとプロフィール

ソースリンクは確認した版に固定し、担当リンクは自分のマージ以外の変更を示します。共同ファイルは単独著作を意味しません。非公開ソースには閲覧権限が必要です。

### 7種の武器動作

Arrow、Laser、Mace、Melody、Pitchfork、Spell、Swordの動作を実装しました。WeaponSystemで装備選択、生成、クールダウン、音を接続しました。通信処理に加えて、実際の戦闘行動を作るクライアント実装です。

[WeaponSystem.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/WeaponSystem.cs), [担当変更 c79844d](https://github.com/YSL-RyuDo/BNB/commit/c79844d425e5c81e1c9549885e29deeeb66515cf), [Arrow.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/Weapon/Arrow.cs), [担当変更 c79844d](https://github.com/YSL-RyuDo/BNB/commit/c79844d425e5c81e1c9549885e29deeeb66515cf), [Sword.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/Weapon/Sword.cs), [担当変更 c79844d](https://github.com/YSL-RyuDo/BNB/commit/c79844d425e5c81e1c9549885e29deeeb66515cf)

### 操作と攻撃タイミング

移動同期と武器攻撃・水風船設置の入力切替を担当しました。Updateで入力を読み、FixedUpdateでRigidbodyを移動します。攻撃要求に武器・位置・向きを渡し、レーザーにはマップ障害物から求めた距離も含めます。

[LocalPlayerController.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/LocalPlayerController.cs), [担当変更 c79844d](https://github.com/YSL-RyuDo/BNB/commit/c79844d425e5c81e1c9549885e29deeeb66515cf)

### 水風船と破壊可能なマップ

水風船設置・クールダウン・重複設置確認・水の伝播、マップとブロック更新を担当しました。サーバーはルームリーダーモデルでワールド更新を扱い、全マップのサーバーシミュレーションとは区別します。

[BalloonSystem.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/BalloonSystem.cs), [担当変更 95b28f2](https://github.com/YSL-RyuDo/BNB/commit/95b28f22b7e88ed2c801e6d47a61ca573bcbc8a5), [MapSystem.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/MapSystem.cs), [担当変更 95b28f2](https://github.com/YSL-RyuDo/BNB/commit/95b28f22b7e88ed2c801e6d47a61ca573bcbc8a5)

### ルーム準備とシーン遷移

キャラクター選択・チーム編成・ホストの開始要求を試合に接続しました。NetworkConnectorで遷移前後の選択、試合シーン移動、キャッシュ状態を扱いました。RoomUIは共同UI内の担当変更として示します。

[RoomSystem.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Room/RoomSystem.cs), [担当変更 be42bb9](https://github.com/YSL-RyuDo/BNB/commit/be42bb99ced515ad702e9a15e9d95bd5e029839c), [NetworkConnector.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Network/NetworkConnector.cs), [担当変更 c79844d](https://github.com/YSL-RyuDo/BNB/commit/c79844d425e5c81e1c9549885e29deeeb66515cf)

### 試合結果、報酬とエモート

GameSystemのプレイヤー生成・被弾・死亡・勝者・報酬、復帰準備を担当しました。エモートの要求・表示・クールダウンも実装しました。結果データを単独画面で終わらせず、プレイ終了と次の流れにつなぎました。

[GameSystem.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/GameSystem.cs), [担当変更 eb6a766](https://github.com/YSL-RyuDo/BNB/commit/eb6a7660b4f690e0d46a7a3e75c50c744d40f745), [EmoticonSystem.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Game/EmoticonSystem.cs), [担当変更 576f512](https://github.com/YSL-RyuDo/BNB/commit/576f51272b078731e71e882afce04ce8ae164ac3)

### アカウント、経済と購入結果

CSV保存を使うサーバーのアカウント、ルーム、ショップ、プロフィール処理を実装しました。共同ShopUIでは購入直後の所有確定を除き、返却データで該当タブを更新しました。価格設計の問いは別のCNNシミュレーション研究へ発展しました。

[Server.cpp (main)](https://github.com/YSL-RyuDo/BnBServer/blob/d2a360c07276ec057a92d1361e8ff84de743520c/TeamOFYServer/Server.cpp), [担当変更 e41da66](https://github.com/YSL-RyuDo/BnBServer/commit/e41da66566252a105da68d98087198e632c0e9b7), [ShopUI.cs (main)](https://github.com/YSL-RyuDo/BNB/blob/8a3defa6001cf741eceb322f11d7c8c6f7dd9bd7/Assets/Script/Shop/ShopUI.cs), [担当変更 57cedd2](https://github.com/YSL-RyuDo/BNB/commit/57cedd2e5c3b0dbbb7ea51b336602cc339630cea)

## 担当した内容

ゲーム・サーバーのパケット連携、水風船・ルームリーダー更新、購入UIを担当しました。

結果・報酬フローとゲームプレイ連携に参加しました。チーム作品であり、全機能・素材の単独制作を意味しません。

本プロジェクトの価格設定問題から発展したCISC-W 2025研究の筆頭著者・ポスター発表者です。

## 貢献の根拠

[Water balloons / 물풍선 / 水風船](https://github.com/YSL-RyuDo/BNB/commit/95b28f22b7e88ed2c801e6d47a61ca573bcbc8a5)

[Packets / 패킷 / パケット](https://github.com/YSL-RyuDo/BNB/commit/c4249cc8ee75e2b4b4ace1bdc02c3f5487076a1f)

[Purchase UI / 구매 UI / 購入UI](https://github.com/YSL-RyuDo/BNB/commit/57cedd2e5c3b0dbbb7ea51b336602cc339630cea)

コミットは記載した変更の根拠であり、貢献率の点数ではありません。外部素材・チームメンバーの貢献は区別します。

## 実装と設計

ルーム、ゲーム内操作、報酬、購入をクライアント・サーバー間で連携させる必要がありました。その実装から、獲得報酬に対してアイテム価格をどう設定するかという疑問が生まれました。

パケット・ゲーム連携、ルームリーダー・水風船更新、ショップ・プロフィール処理を担当しました。別のCISC-W研究では仮想プレイヤーの行動を用い、固定価格と需要連動政策を比較しました。

## コードの動作と実装構造の理由

確認した実装が扱う制約と利点・代償を説明します。設計効果の説明はコード分析であり、比較する全代案を当時試作したという意味ではありません。担当リンクは共同制作内の変更を示し、ソースの閲覧権限は維持します。

### 購入クリックだけでは所有状態を変えない理由

**実装の流れ**

私のShopUI変更では、BuyItemが購入要求を送りボタンを無効にする一方、直ちにselectedOwnedをtrueにする処理を削除しました。水風船、エモート、アイコンのデータ到着時には、対応するタブが選択中の場合に表示を更新するよう変更しました。

**この構造が必要な理由**

クライアントが知っているのはクリックの事実であり、サーバーの購入承認ではありません。返された商品データに合わせれば、推測した所有状態を確定扱いする問題を減らせます。現在のタブも確認することで、別カテゴリの遅延応答が表示中の一覧を置き換えるのを避けます。

**代案と現在の実装の折合い**

クリック直後の所有表示は速い一方、失敗時に正確な巻戻しが必要です。サーバーの商品データ待ちは遅延が見える代わりに所有を早まって確定しません。この変更は後者です。処理中表示なら取引完了を装わず入力への反応を示せます。

**利点・代償と検証範囲**

これは共同クライアント内の担当変更であり、ShopUI全体の単独実装を意味しません。所有表示を待つ分、失敗とタイムアウト対応も重要ですが、この差分だけで再試行処理の完成は示せません。後のVAISでは取引層でも再要求を扱います。

[BNB 57cedd2e](https://github.com/YSL-RyuDo/BNB/commit/57cedd2e5c3b0dbbb7ea51b336602cc339630cea)

### ゲーム同期と価格研究で異なる判断の対象

**実装の流れ**

私のマルチプレイヤー変更は武器操作、水風船更新、ルームリーダー情報をクライアントのネットワークメッセージにつなぎます。C++サーバーは接続ごとのスレッドとCSVでルーム、アカウント、ショップを扱います。アイテム価格の検討は別途CNNによる消費傾向の研究につながり、I2P-CON発表後に補完してCISC-Wポスターで発表しました。

**この構造が必要な理由**

一部のワールド判断をルームリーダーから送る方式はサーバー側のシミュレーション範囲を減らしますが、そのクライアントへの信頼と切断対応が条件になります。価格は別の問題で、購入機能の動作だけでは適切さは分かりません。シミュレーションでは報酬や消費の仮定を明示し、同じ条件で政策を比較できます。

**代案と現在の実装の折合い**

全ワールドのサーバー計算は制御を広げる代わりに多くの規則をサーバーにも実装します。ルームリーダー入力は実装範囲を減らしクライアントへの信頼を受け入れます。固定価格は説明しやすく、需要連動価格は仮定と安定性の検討が増えます。研究は比較であり動的価格の普遍的優位を示すものではありません。

**利点・代償と検証範囲**

ゲーム全体をサーバー権威型シミュレーションとは説明しません。論文の仮想実験は実利用者の測定とは異なり、CNN価格モデルをBnBServerで運用した証拠でもありません。実装と研究はそれぞれの根拠で区別します。

[BNB 95b28f22](https://github.com/YSL-RyuDo/BNB/commit/95b28f22b7e88ed2c801e6d47a61ca573bcbc8a5), [BnBServer/ClientHandler.cpp](https://github.com/YSL-RyuDo/BnBServer/blob/5f5f067f7cdc6ef71ab11a4d869ad2e1b6fdc8a2/TeamOFYServer/ClientHandler.cpp), [BnBServer/StoreData.cpp](https://github.com/YSL-RyuDo/BnBServer/blob/5f5f067f7cdc6ef71ab11a4d869ad2e1b6fdc8a2/TeamOFYServer/StoreData.cpp)

## ゲーム開発から研究へ

**CISC-W 2025 ポスター発表。** *Consumption Pattern Analysis of In-Game Pricing Policies Using Convolutional Neural Networks.* 筆頭著者・発表者：Young-Sang Lee。共著者：Ji-Uk Choi、Yun-Ho Kim、Myeong-Jae Lee、Hye-Young Kim。

Slap Squadの試合報酬に対するアイテム価格を検討したことが研究の出発点です。論文中のゲーム名は**SlapSquad**です。仮想プレイヤー400人・30日間のシミュレーションを20回繰り返し、固定価格と基準価格の70～130%に制限した需要連動価格を比較しました。報告されたCNN精度96%は、シミュレーション上の消費パターンを価格政策別に分類した性能であり、最適価格の精度や実測売上増加率ではありません。

[論文PDF](research/CISC-W_2025_paper.pdf)。本ゲームが研究の契機ですが、動的政策の実サーバーへの適用はここでは実証していません。

## 実行・コードの確認

[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)記載のUnity版で開いてください。パッケージロックとシリアライズ済み素材を維持し、Unityを開く前に対象ブランチを選んでください。

Unity scene: [Assets/Scenes/LoginScene.unity](Assets/Scenes/LoginScene.unity)

[BnBServer](https://github.com/YSL-RyuDo/BnBServer)

## 開発・検証範囲

過去の実装を整理したプロジェクトです。一部のワールド判断にルームリーダー側の入力を使うため、完全なサーバー権威型とは説明しません。価格研究は仮想プレイヤーによる実験で、分類精度は最適価格や実サービス売上の改善を意味しません。研究上の政策と実際のサーバー政策を同一視しません。

CNN価格政策研究を「データで設計するゲーム経済」として発表。その後、研究を補完してCISC-W 2025のポスター発表につなげました。

[CISC-W 2025](https://ysl-ryudo.github.io/ja/research/cisc/), [I2P-CON](https://www.youtube.com/watch?v=oq_e0TAwvJg)

## 関連リポジトリ

[BNB](https://github.com/YSL-RyuDo/BNB), 公開リポジトリ

[BnBServer](https://github.com/YSL-RyuDo/BnBServer), 公開リポジトリ

**CISC-W 2025: 筆頭著者、ポスター発表**

[2025年 韓国情報保護学会 冬季学術大会](https://kiisc.or.kr/payment/pay/292)

2025.11.27 ~ 28, 韓国・京畿道広州、昆池岩リゾート
