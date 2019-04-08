# ILEditorとは？
IBMi専用の無料で使用できる開発環境(IDE)です。
IBMiのソースをPCにダウンロードして編集したりコンパイルが出来きたりするのですが、残念ながら日本語のソースは文字化けして正しく動きません。
日本語のソースを文字化けしないように改修したので記事にしました。

# インストール方法
1. [本家のサイト](https://worksofbarry.com/ileditor/)からダウンロードしてインストールして下さい。
2. [ここ](https://github.com/ymurata1967/ILEditor/releases/tag/1.6.7.0Jp)からexeファイルをダウンロードし、インストールされた同じフォルダに置いて下さい。
なお、オリジナルを消したくない場合はILEditorJp.exe等適当に名前を変更して同フォルダに置いて下さい。

# 文字化け対応方法
以前に[IBMiCmdをShiftJis対応してみた](https://qiita.com/yamurata/items/7313094a0080f4f74283)という記事で、notepad++のpluginをやはり文字化け対応したのですが、同じ方法では駄目でした。
notepad++の時はftpのダウンロードやアップロード時に日本語が通るようにしていたのですが、ILEditorではFluentFTPというC#ネイティブのFTPクライアントライブラリが使用されています。

当初はFluentFTP側で対処出来ないかテストしていたのですがどうも上手く動かず、
仕方なくIBMi側でコード変換してからバイナリモードでダウンロード、アップロードするようにしました。

基本的には、
1. 対象のソースをCPYコマンドでIFSにコピー。
2. コピーされたソースをCPYコマンドでコード変換（CCSID=943）。
3. バイナリモードでダウンロード。
という方法で実現しています。

アップロードはこの逆を行います。

# ワークディレクトリ
上記の実装にしたため、IFS上にコード変換用のワークファイルを作成します。
/tmpフォルダ配下に
/ILEDITOR/<ユーザー名>/
フォルダを作成し、そこにワークファイルを作成します。

※ILEDITORを使用しなくなった際は手動で削除して下さい。

# エディタ部の文字コード
Shift-Jisにしています。
なお、日本語フォントを追加しました。（ＭＳ ゴシック、ＭＳ 明朝）
Connetion SettingsのEditorタブで指定出来ます。

# IFSにあるファイルの表示／保存
ILEditorではIFSにあるテキストファイルが表示できますが、エディタ部はShift-Jis固定なので
IFS上にあるファイルもいったんCCSID=943に変換してからダウンロードします。

SAVEするとIFSに反映されますがこの時はUTF-8で保存する仕様としました。
本来はダウンロードする際に元の文字コードを保持し、そのままアップロードするべきですが、
そこまで気力が無く・・・。

という事で、IFSを保存（アップロード）する際は気を付けてください。

# 日本語化できなかった部分
1. 新しいソースメンバーを作る事が出来ますが、その際に入力するテキストに日本語を入れると化けます。
2. コンパイルエラーの時にジョブログを表示する「show job log on compile」というオプションがあるのですが、チェックを入れると動きがおかしくなるようで、日本語はしませんでした。

# ソースを表示する際にエラーが出る場合
文字コード変換はIBMiで行いますがコード変換に失敗するとソース表示時にエラーが発生します。
![ileditor01.PNG](https://qiita-image-store.s3.amazonaws.com/0/146069/e152d581-3b65-12cc-634e-8ad5fd04e1cf.png)
（例えば、シフトアウト有り／シフトイン無しのソースだと変換エラーになります。）
Help→Session FTP Logでログが表示されるので、エラーが発生したコマンドを5250から実行してみる、WRKACTJOBのJOBLOGからエラーのコマンドを特定する、等でエラーの原因を特定してみて下さい。

# ログインするユーザーのCCSID
5026でも5035でも問題無いかと思いますが、ソースのCCSIDとログインユーザーのCCSIDは合わせてください。
また65535はダメなので注意して下さい。
（一部、SQLのREPLACE関数を使用していますが65535だと実行できないようです。）

# 応用編（バージョン管理）
ダウンロードに使用されているフォルダをバージョン管理対象にすれば可能かと思われます。

ILEditorは
C:\Users\ユーザー名\AppData\Roaming\ILEditorData\source\システム名\ライブラリ名\ソースファイル名\メンバー名
という階層でデータを保持します。

以下はSVNで変更差分を取ってみた例です。
![ileditor02.PNG](https://qiita-image-store.s3.amazonaws.com/0/146069/0581da14-0277-c39a-54f0-2898601dfa31.png)

なお、このツールを使用する事により障害等が発生しても一切の責任を負いません。自己責任でお願い致します。
海外は良いツールが沢山ありますねぇ。

