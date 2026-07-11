# v1.0.0 - 霧 for YMM4

YukkuriMovieMaker4 向けの霧エフェクトプラグインの初回リリースです。
Direct2D カスタムピクセルシェーダーがシーン座標のフラクタルノイズから霧の密度を手続き的に生成し、光学的な厚みに対する透過率で元映像へ合成します。
参照画像や外部モデルを使わず、奥行き推定で遠い位置ほど霧を濃くし、光源方向へのレイマーチングで霧を照らします。
ノイズは座標・時刻・シード値から決定論的に決まります。
8 言語リソース構成の UI を備えます。

---

## 新機能

### 1. ピクセルシェーダー

`Fog.hlsl` の `main` はシーン座標のフラクタルノイズから霧の密度を求め、光学的な厚みに対する透過率で元映像を減衰させながら霧色を重ねます。追加テクスチャは使用しません。`density <= 0` または `source.a <= 0` のときはソースをそのまま返します。乱数は `Hash.hlsli` の `hash13` と `hash21` を用います。

#### 密度場

`DensityAt` はシーン座標に `invFeature` を掛けたサンプル位置で値ノイズ `Noise3` を評価し、`Fbm` で複数オクターブを重ねます。オクターブ数が 4 以上のときはドメインワープで塊状のむらを作ります。高さ方向の偏りは `hf = saturate(1 + gradient × (yn × 2 − 1))` で与えます。むらは `pow` と `lerp` で濃淡の差を制御し、`unevenness` が 0 のときは高さ方向の偏りだけを返します。

| 項目 | 説明 |
|---|---|
| `invFeature` | スケールから求めるノイズの周波数 |
| `flowX` / `flowY` | 時刻に掛けてサンプル位置をずらす流れ |
| `boilSpeed` | 時刻に掛けてノイズの断面を進める変化速度 |
| `seed` | サンプル位置と断面をずらすシード値 |
| `hf` | 高さによる濃度の偏り |

#### 光学的な厚みと透過率

本体画素の密度 `d` を 5 オクターブで求め、`opticalDepth = d × density × 3 × depthFactor` を厚みとします。透過率は `transmittance = exp(−opticalDepth)` です。厚みが大きいほど元映像を強く減衰させ、霧色を強く重ねます。

#### 奥行き推定

`depthAmount` が 0 より大きいとき `EstimateDepth` が奥行きを求め、`depthFactor = lerp(1, depth, depthAmount)` として厚みへ掛けます。奥行きは消失点からの距離に基づく値と、暗チャンネルによる霞み具合に基づく値を `hazeMix` で補間します。

| 値 | 説明 |
|---|---|
| `depthV` | 消失点から最遠隅までの距離で正規化した放射状の奥行き |
| `depthD` | `DarkChannel` の 3×3 最小値から求める霞み由来の奥行き |
| `hazeMix` | `depthV` と `depthD` を混ぜる割合 |

#### 光の散乱

`sunIntensity` が 0 より大きいとき、光源方向 `float2(cos(sunAngle), sin(sunAngle))` へ `SUN_STEPS` 回サンプル位置を進めながら密度 `tau` を積算します。`sunTransmittance = exp(−tau × …)` を求め、`airlight` へ光の色を加算します。霧の光源側が明るく縁取られます。

| 関数 | サンプル数 | 方向 | 説明 |
|---|---|---|---|
| `DensityAt`（本体） | 1 | — | 5 オクターブの霧の密度 |
| `DensityAt`（光） | `SUN_STEPS` = 6 | 光源方向 | 3 オクターブで積算する光路上の密度 |

#### 合成

霧の色 `airlight` は霧色を基準とし、光を加えた後に `saturate` で丸めます。出力は入力アルファを保持し、プリマルチプライドで返します。

| 項目 | 式 |
|---|---|
| 透過率 | `exp(−opticalDepth)` |
| 霧色 | `saturate(fogColor + sunColor × sunTerm)` |
| 出力色 | `source.rgb × transmittance + airlight × (1 − transmittance) × source.a` |

最終出力は `result.rgb` を入力アルファでクランプし、プリマルチプライドを保ちます。

---

### 2. カスタムシェーダーエフェクト

`FogCustomEffect` は `[CustomEffect(1)]` の 1 入力エフェクトです。公開プロパティは `SetValue` を介して定数バッファーへ転送します。各プロパティは代入時にシェーダーが前提とする範囲へ制限します。

| プロパティ | 型 | 範囲 |
|---|---|---|
| `Density` | `float` | 0〜10 |
| `InvFeature` | `float` | 1e-5〜1 |
| `Time` | `float` | 制限なし |
| `Unevenness` | `float` | 0〜1 |
| `FlowX` | `float` | 制限なし |
| `FlowY` | `float` | 制限なし |
| `BoilSpeed` | `float` | 制限なし |
| `Gradient` | `float` | -1〜1 |
| `FogR` | `float` | 0〜1 |
| `FogG` | `float` | 0〜1 |
| `FogB` | `float` | 0〜1 |
| `Seed` | `float` | 制限なし |
| `SunIntensity` | `float` | 0〜10 |
| `SunAngle` | `float` | ラジアン |
| `SunR` | `float` | 0〜1 |
| `SunG` | `float` | 0〜1 |
| `SunB` | `float` | 0〜1 |
| `DepthAmount` | `float` | 0〜1 |
| `VpX` | `float` | -65536〜65536 |
| `VpY` | `float` | -65536〜65536 |
| `HazeMix` | `float` | 0〜1 |

`ConstantBuffer` のレイアウトは以下のとおりです。末尾に 3 つの詰め物を置き、合計サイズを 16 バイトの倍数に揃えます。

| フィールド | 型 | 説明 |
|---|---|---|
| `Density` | `float` | 濃度 |
| `InvFeature` | `float` | ノイズの周波数 |
| `Time` | `float` | 時刻（秒） |
| `Unevenness` | `float` | むら |
| `FlowX` | `float` | 流れの横成分 |
| `FlowY` | `float` | 流れの縦成分 |
| `BoilSpeed` | `float` | 変化速度 |
| `Gradient` | `float` | 高低差 |
| `FogR` | `float` | 霧色 R |
| `FogG` | `float` | 霧色 G |
| `FogB` | `float` | 霧色 B |
| `Seed` | `float` | シード値 |
| `SunIntensity` | `float` | 光の強さ |
| `SunAngle` | `float` | 光の角度（ラジアン） |
| `InputTop` | `float` | 入力矩形の上 |
| `InputHeight` | `float` | 入力矩形の高さ |
| `SunR` | `float` | 光色 R |
| `SunG` | `float` | 光色 G |
| `SunB` | `float` | 光色 B |
| `DepthAmount` | `float` | 奥行き |
| `VpX` | `float` | 消失点 X |
| `VpY` | `float` | 消失点 Y |
| `HazeMix` | `float` | 霞検出 |
| `InputLeft` | `float` | 入力矩形の左 |
| `InputWidth` | `float` | 入力矩形の幅 |
| `Pad0`〜`Pad2` | `float` | 詰め物 |

`MapInputRectsToOutputRect` は入力 0 の矩形を出力矩形に設定し、入力矩形の位置とサイズを定数バッファーに書き込みます。`MapOutputRectToInputRects` は近傍サンプルに必要な 3px 分だけ入力矩形を拡張します。

シェーダーリソース: `pack://application:,,,/Fog;component/Shaders/Fog.cso`（ps_5_0、`ShaderResourceUri.Get` が生成）

---

### 3. エフェクト定義

`FogEffect` は YMM4 の映像エフェクトとして宣言されます。

`[VideoEffect]` 属性は以下のパラメーターで宣言されます。

- 表示名：`Texts.FogEffectName`（ローカライズキー）
- カテゴリー：`VideoEffectCategories.Filtering`
- 検索タグ：`TagFog`・`TagMist`・`TagWeather`
- `IsAviUtlSupported = false` により AviUtl 向け EXO 出力は非対応
- `ResourceType = typeof(Texts)` でローカライズリソースを指定

`Label` プロパティは `Texts.FogEffectName` を返します。

公開プロパティは以下のとおりです。

| プロパティ | 型 | デフォルト | 内部範囲 | アニメーション |
|---|---|---|---|---|
| `Density` | `Animation` | 50 | 0〜500 | あり |
| `Scale` | `Animation` | 100 | 1〜2000 | あり |
| `Unevenness` | `Animation` | 50 | 0〜100 | あり |
| `Gradient` | `Animation` | 0 | -100〜100 | あり |
| `FlowSpeed` | `Animation` | 30 | -1000〜1000 | あり |
| `FlowAngle` | `Animation` | 0 | -36000〜36000 | あり |
| `ChangeSpeed` | `Animation` | 20 | -1000〜1000 | あり |
| `Seed` | `int` | 0 | 0〜int.MaxValue | なし |
| `FogColor` | `Color` | `#FFDCE1E6` | — | なし |
| `DepthAmount` | `Animation` | 0 | 0〜100 | あり |
| `HazeDetect` | `Animation` | 30 | 0〜100 | あり |
| `VanishingPointX` | `Animation` | 0 | — | あり |
| `VanishingPointY` | `Animation` | 0 | — | あり |
| `SunIntensity` | `Animation` | 0 | 0〜500 | あり |
| `SunAngle` | `Animation` | 270 | -36000〜36000 | あり |
| `SunColor` | `Color` | `#FFFFF2D8` | — | なし |

`GetAnimatables` は `Density`・`Scale`・`Unevenness`・`Gradient`・`DepthAmount`・`VanishingPointX`・`VanishingPointY`・`HazeDetect`・`FlowSpeed`・`FlowAngle`・`ChangeSpeed`・`SunIntensity`・`SunAngle` を返します。

`HazeDetect`・`VanishingPointX`・`VanishingPointY` は `DepthSettingsVisibleAttribute` により、`DepthAmount` が 0 より大きいときにだけ UI に表示されます。

`CreateExoVideoFilters` は空のシーケンスを返します（EXO 非対応）。`CreateVideoEffect` は映像処理用のインスタンスを生成します。

---

### 4. フレームごとの更新

各フレームで YMM4 の `EffectDescription` からフレーム位置、アイテム長、FPS を取得し、アニメーション値を評価します。前フレームと値が異なる項目だけをカスタムシェーダーへ転送します。`Time` はフレーム位置と FPS から毎フレーム更新します。

| パラメータ | 変換 |
|---|---|
| `Density` | `value / 100` に霧色の不透明度を掛ける |
| `Scale` | `1 / (1.5 × max(value, 1))` を `InvFeature` へ |
| `Unevenness` | `value / 100` |
| `Gradient` | `value / 100` |
| `DepthAmount` | `value / 100` |
| `VanishingPointX` / `VanishingPointY` | px のまま |
| `HazeDetect` | `value / 100` を `HazeMix` へ |
| `FlowSpeed` / `FlowAngle` | `cos/sin(角度) × (速さ / 100)` を `FlowX`・`FlowY` へ |
| `ChangeSpeed` | `value / 100` を `BoilSpeed` へ |
| `SunIntensity` | `value / 100` に光色の不透明度を掛ける |
| `SunAngle` | 度からラジアンへ変換 |
| `FogColor` | `R/G/B` を 0〜1 の float へ変換し、A は濃度へ反映 |
| `SunColor` | `R/G/B` を 0〜1 の float へ変換し、A は光の強さへ反映 |
| `Seed` | 整数のまま |
| `Time` | `frame / fps` |

`DepthAmount` が正の値を持つとき、消失点をドラッグで動かす操作点をプレビューに追加します。入力は `SetInput(0, input, true)` でカスタムシェーダーへ接続します。エフェクトチェーンのクリア時は入力 0 を `null` に戻します。

---

### 5. ローカライズ

`Texts` クラスは `[AutoGenLocalizer]` 属性を持つ `partial` クラスとして宣言されます。
`YukkuriMovieMaker.Generator` のソースジェネレーターが `Texts.csv` を処理し、各ロケールのリソースファイルを自動生成します。

対応リソース：日本語（`ja-jp`）・英語（`en-us`）・中国語簡体字（`zh-cn`）・中国語繁体字（`zh-tw`）・韓国語（`ko-kr`）・スペイン語（`es-es`）・アラビア語（`ar-sa`）・インドネシア語（`id-id`）

ローカライズキーの一覧は以下のとおりです。

| キー | ja-jp |
|---|---|
| `FogEffectName` | 霧 |
| `TagFog` | 霧 |
| `TagMist` | もや |
| `TagWeather` | 天候 |
| `FogDensity` | 濃度 |
| `FogScale` | スケール |
| `FogUnevenness` | むら |
| `FogGradient` | 高低差 |
| `FogFlowSpeed` | 流れの速さ |
| `FogFlowAngle` | 流れの角度 |
| `FogChangeSpeed` | 変化速度 |
| `FogSeed` | シード値 |
| `FogColor` | 霧の色 |
| `FogDepthGroup` | 奥行き |
| `FogDepthAmount` | 奥行き |
| `FogHazeDetect` | 霞検出 |
| `FogVanishingPointX` | 消失点X |
| `FogVanishingPointY` | 消失点Y |
| `FogLightGroup` | 光の設定 |
| `FogSunIntensity` | 光の強さ |
| `FogSunAngle` | 光の角度 |
| `FogSunColor` | 光の色 |
