# 霧 for YMM4

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](#)
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)](#)
[![Release](https://img.shields.io/github/v/release/routersys/YMM4-Fog.svg)](https://github.com/routersys/YMM4-Fog/releases)

---

YukkuriMovieMaker4（YMM4）上で動作する、霧を手続き的に生成して入力映像へ合成する映像エフェクトプラグインです。
参照画像や外部モデルを使わず、シーン座標のノイズだけから霧の濃淡を作ります。
霧の光学的な厚みに応じて元映像を減衰させ、奥から差し込む光で霧を照らします。
数値パラメータはアニメーションに対応しています。

![Image](https://github.com/routersys/YMM4-Fog/blob/main/docs/Fog.png)

---

## 目次

1. [概要](#概要)
2. [動作要件](#動作要件)
3. [インストール方法](#インストール方法)
4. [主な機能](#主な機能)
   - [1. 手続き的な霧の生成](#1-手続き的な霧の生成)
   - [2. 流れと変化](#2-流れと変化)
   - [3. 奥行きによる濃度変化](#3-奥行きによる濃度変化)
   - [4. 光の散乱](#4-光の散乱)
   - [5. シンプルな調整項目](#5-シンプルな調整項目)
5. [パラメータ一覧](#パラメータ一覧)
6. [制限事項](#制限事項)
7. [注意事項](#注意事項)
8. [免責事項](#免責事項)
9. [サードパーティライセンス](#サードパーティライセンス)
10. [ライセンス](#ライセンス)

---

## 概要

本プラグインは YMM4 の「映像エフェクト」として動作し、エフェクトの種類一覧では「霧」として表示されます。カテゴリはフィルタリングです。

Direct2D カスタムピクセルシェーダーがシーン座標のフラクタルノイズから霧の密度を求め、その密度から光学的な厚みを計算します。厚みが大きいほど元映像を強く減衰させ、代わりに霧の色を重ねます。奥行きを有効にすると、遠い位置ほど霧を濃くします。光を有効にすると、光源の方向へ霧の中を進みながら濃度を積算し、光が届く量に応じて霧を明るく染めます。

ノイズはシード値とシーン座標と時刻から決定論的に生成します。時刻に依存する項は流れと変化速度だけなので、これらが 0 のとき霧は静止し、同じ条件では常に同じ結果になります。

このエフェクトは AviUtl 向けの EXO 出力（`.exo`）には対応していません。

---

## 動作要件

| 項目 | 要件 |
|---|---|
| OS | Windows 10 バージョン 2004（ビルド 19041）以降 / Windows 11（64bit） |
| YukkuriMovieMaker4 | 最新版を推奨 |
| ランタイム | .NET 10.0 |

---

## インストール方法

1. [Releases](https://github.com/routersys/YMM4-Fog/releases/latest) ページから最新のプラグインファイル（`.ymme`）をダウンロードしてください。
2. YMM4 が起動していないことを確認し、ダウンロードしたファイルを実行してインストールします。
3. YMM4 を起動し、タイムライン上のアイテムに映像エフェクトを追加します。
4. 映像エフェクトの種類として「霧」を選択してください。

---

## 主な機能

### 1. 手続き的な霧の生成

シーン座標のフラクタルノイズから霧の密度を求めます。手動の点指定や参照画像は不要です。密度から光学的な厚みを計算し、透過率で元映像を減衰させながら霧の色を重ねます。スケールで霧のむらの大きさ、むらで濃淡の差、高低差で高さ方向の濃度の偏りを調整します。

### 2. 流れと変化

流れの速さと角度で霧全体を平行移動させます。変化速度で霧の形そのものをゆっくり変えます。流れの速さと変化速度をどちらも 0 にすると、霧は動かず静止します。シード値を変えると霧の形が変わります。

### 3. 奥行きによる濃度変化

奥行きを上げると、推定した奥行きに応じて遠い位置ほど霧を濃くします。奥行きは消失点からの距離と、元映像に元から含まれる霞み具合の 2 つから推定します。霞検出でどちらを重視するかを決めます。消失点はプレビュー上の操作点をドラッグして指定できます。奥行きが 0 のとき、霞検出と消失点の項目は表示されません。

### 4. 光の散乱

光の強さを上げると、光源の方向へ霧の中を進みながら濃度を積算し、光が届く量に応じて霧を明るく染めます。霧の光源側が明るく縁取られます。光の角度で差し込む方向、光の色で色みを決めます。

### 5. シンプルな調整項目

濃度、スケール、むら、高低差、流れ、変化速度、奥行き、光だけで調整できます。複雑なマスクや素材の用意は不要です。数値項目はアニメーションに対応しています。

---

## パラメータ一覧

| パラメータ名 | 型 | デフォルト | スライダー表示範囲 | アニメーション | 説明 |
|---|---|---|---|---|---|
| 濃度 | 数値 | 50% | 0 〜 100% | ✔ | 霧の濃さです。色の不透明度も濃度に掛かります。 |
| スケール | 数値 | 100% | 10 〜 500% | ✔ | 霧のむらの大きさです。 |
| むら | 数値 | 50% | 0 〜 100% | ✔ | 濃淡の差です。高いほど霧が塊状になり切れ間が生まれます。 |
| 高低差 | 数値 | 0% | -100 〜 100% | ✔ | 高さによる濃度の偏りです。正で下に濃い地霧、負で上に濃い靄になります。 |
| 流れの速さ | 数値 | 30% | -200 〜 200% | ✔ | 霧が平行移動する速さです。 |
| 流れの角度 | 数値 | 0° | 0 〜 360° | ✔ | 霧が流れる方向です。 |
| 変化速度 | 数値 | 20% | -200 〜 200% | ✔ | 霧の形が変化する速さです。 |
| シード値 | 整数 | 0 | 0 〜 10000 | ✗ | 霧の形を決める乱数のシード値です。 |
| 霧の色 | 色 | `#FFDCE1E6` | — | ✗ | 霧の色です。不透明度は濃度に反映されます。 |
| 奥行き | 数値 | 0% | 0 〜 100% | ✔ | 推定した奥行きに応じて遠い位置ほど霧を濃くします。 |
| 霞検出 | 数値 | 30% | 0 〜 100% | ✔ | 元映像の霞み具合から奥行きを推定する割合です。 |
| 消失点X | 数値 | 0px | -500 〜 500px | ✔ | 最も遠いと見なす横位置です。プレビューの操作点をドラッグして指定できます。 |
| 消失点Y | 数値 | 0px | -500 〜 500px | ✔ | 最も遠いと見なす縦位置です。 |
| 光の強さ | 数値 | 0% | 0 〜 100% | ✔ | 霧を照らす光の強さです。色の不透明度も強さに掛かります。 |
| 光の角度 | 数値 | 270° | 0 〜 360° | ✔ | 光が差し込む方向です。 |
| 光の色 | 色 | `#FFFFF2D8` | — | ✗ | 霧を照らす光の色です。 |

霞検出と消失点は、奥行きが 0 より大きいときにだけ表示されます。

---

## 制限事項

- AviUtl 向けの EXO 出力（`.exo`）には対応していません。
- 透明部分（アルファが 0）には霧が適用されません。

---

## 注意事項

- AviUtl 非対応: 本プラグインは AviUtl 向けの EXO 出力（`.exo`）に対応していません。
- 透明部分の扱い: ソース画像の透明部分（アルファが 0）はそのまま出力され、霧や光は透明部分へ出ません。
- 霧の再現性: ノイズは座標・時刻・シード値から生成するため、同じ条件では常に同じ結果になります。流れの速さと変化速度が 0 のとき、霧は時間で変化しません。
- 消失点の操作点: 奥行きが 0 より大きいとき、プレビューに消失点の操作点が表示されます。
- 本プラグインを使用する前に、YMM4 プロジェクトファイルのバックアップを作成することを推奨します。

---

## 免責事項

本プラグインは MIT ライセンスのもとで公開されています。

本ソフトウェアは「現状のまま」提供されており、明示・黙示を問わず、商品性、特定目的への適合性、および権利非侵害に関する保証を含む、いかなる種類の保証も行いません。

作者は、本プラグインの使用または使用不能に起因するいかなる損害についても、一切の責任を負いません。
ご利用は自己責任でお願いします。

---

## サードパーティライセンス

本プラグインは以下のサードパーティのコードを使用しています。ライセンスの全文は、リポジトリの [`Fog/shaders/Hash.hlsli`](Fog/shaders/Hash.hlsli) の冒頭に収録しています。

| ソフトウェア | 用途 | ライセンス |
|---|---|---|
| [Hash without Sine](https://www.shadertoy.com/view/4djSRW) | 霧のノイズ生成に使うハッシュ関数（`Hash.hlsli`） | MIT License |

### Hash without Sine（MIT License）

`Fog/shaders/Hash.hlsli` は David Hoskins 氏の "Hash without Sine" を基にし、饅頭遣い（manju-summoner）氏が改変したものです。以下は同ファイルに収録された著作権表示です。

```
Copyright (c)2014 David Hoskins.
Modifications Copyright (c) 2023 manju-summoner.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

---

## ライセンス

[MIT License](LICENSE.txt)
