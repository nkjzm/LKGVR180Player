using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Kirurobo;
using LookingGlass;
using SFB;

public class QuiltFileLoader : MonoBehaviour
{
    WindowController window;
    Texture2D texture;
    Quilt.Settings defaultSetting;

    public Text messageText;

    /// <summary>
    /// 読み込み待ちならtrueにする
    /// </summary>
    bool isLoading = false;

    /// <summary>
    /// メッセージを表示した場合、それを消去する時刻[s]を入れる
    /// </summary>
    float messageClearTime = 0;

    /// <summary>
    /// スライドショー対象の指定ファイル。
    /// これが空ならば現在開いたファイルと同じディレクトリを探す。
    /// </summary>
    List<string> targetFiles = new List<string>();

    /// <summary>
    /// 現在表示されている画像ファイルのパス
    /// </summary>
    string currentFile;

    Holoplay holoplay;

    // Use this for initialization
    void Start()
    {
        // ファイルドロップなどを扱うためのWindowControllerインスタンスを取得
        window = FindObjectOfType<WindowController>();
        window.OnFilesDropped += Window_OnFilesDropped;

        // Quiltのインスタンスを取得
        holoplay = FindObjectOfType<Holoplay>();
        defaultSetting = holoplay.quiltSettings;   // Tilingの初期設定を記憶しておく

        // フレームレートを下げる
        Application.targetFrameRate = 15;

        // サンプルの画像を読み込み
        LoadFile(Path.Combine(Application.streamingAssetsPath, "startup.png"), true);
    }

    void Update()
    {
        // 操作できるのはファイル読み込み待ちでないときだけ
        if (!isLoading)
        {
            // [O] キーまたは右クリックでファイル選択ダイアログを開く
            if (Input.GetKey(KeyCode.O) || Input.GetMouseButton(1))
            {
                OpenFile();
            }

            // [S] キーで現在の画面を保存
            if (Input.GetKey(KeyCode.S))
            {
                SaveFile();
            }

            //// [T] キーでウィンドウ透過
            //if (Input.GetKey(KeyCode.T))
            //{
            //    if (window)
            //    {
            //        window.isTransparent = !window.isTransparent;
            //    }
            //}

            // 前の画像
            if (ButtonManager.GetButton(ButtonType.LEFT) || Input.GetKey(KeyCode.LeftArrow))
            {
                ShowMessage("");    // ファイル名が表示されていれば消す
                LoadFile(GetNextFile(-1));
            }

            // 次の画像
            if (ButtonManager.GetButton(ButtonType.RIGHT) || Input.GetKey(KeyCode.RightArrow))
            {
                ShowMessage("");    // ファイル名が表示されていれば消す
                LoadFile(GetNextFile(1));
            }

            if (ButtonManager.GetButton(ButtonType.CIRCLE))
            {
                ShowMessage(currentFile);
            }
        }

        // メッセージを一定時間後に消去
        if (messageClearTime > 0)
        {
            if (messageClearTime < Time.time)
            {
                messageText.text = "";
                messageClearTime = 0;
            }
        }
    }

    /// <summary>
    /// 一定時間で消えるメッセージを表示
    /// </summary>
    /// <param name="text"></param>
    private void ShowMessage(string text)
    {
        const float lifetime = 5f;  // 消去までの時間[s]

        if (messageText)
        {
            messageText.text = text;
            messageClearTime = Time.time + lifetime;
        }
    }

    /// <summary>
    /// 現在の画面をPNGで保存
    /// </summary>
    private void SaveFile()
    {
        // 現在のRenderTextureの内容からTexture2Dを作成
        RenderTexture renderTexture = RenderTexture.active;
        int w = Screen.width;
        int h = Screen.height;
        Texture2D texture = new Texture2D(w, h, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        texture.Apply();

        // PNGに変換
        byte[] rawData = texture.EncodeToPNG();
        Destroy(texture);

        // 日時を基にファイル名を決定
        string file = "LookingGlass_" + System.DateTime.Now.ToString("yyyyMMdd_hhmmss") + ".png";

        // 書き出し
        System.IO.File.WriteAllBytes(file, rawData);
        Debug.Log("Saved " + file);

        // 保存したというメッセージを表示
        ShowMessage("Saved " + file);
    }

    /// <summary>
    /// 画像を読み込み
    /// </summary>
    /// <param name="uri">Path.</param>
    private void LoadFile(string path, bool isImage = false)
    {
        if (string.IsNullOrEmpty(path)) return;

        isLoading = true;
        currentFile = path;

        if (isImage)
        {
            string uri = new System.Uri(path).AbsoluteUri;
            Debug.Log("Loading: " + path);
            StartCoroutine("LoadFileCoroutine", uri);
        }
        else
        {
            StartCoroutine("LoadVR180Coroutine", path);
            Debug.Log("LoadFileCoroutine" + path);
        }
    }


    /// <summary>
    /// コルーチンでファイル読み込み
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    IEnumerator LoadVR180Coroutine(string file)
    {
        FindObjectOfType<VR180Mesh>().GenerateMesh(file);
        FindObjectOfType<VR180Mesh>().StartPlay(file);

        // 前のtextureを破棄
        Destroy(texture);

        // Quiltを読み込み
        holoplay.customQuiltSettings = new Quilt.Settings(
               2560, 1440,
               2, 1, 2
               );
        holoplay.overrideQuilt = ToTexture2D(renderTexture);
        holoplay.SetupQuilt();

        // 念のため毎回GCをしてみる…
        System.GC.Collect();

        // Debug.Log("Estimaged tiling: " + quilt.tiling.presetName);     // 選択されたTiling

        // 読み込み完了
        isLoading = false;

        yield return null;
    }

    public static Texture2D ToTexture2D(Texture self)
    {
        var sw = self.width;
        var sh = self.height;
        var format = TextureFormat.RGBA32;
        var result = new Texture2D(sw, sh, format, false);
        var currentRT = RenderTexture.active;
        var rt = new RenderTexture(sw, sh, 32);
        Graphics.Blit(self, rt);
        RenderTexture.active = rt;
        var source = new Rect(0, 0, rt.width, rt.height);
        result.ReadPixels(source, 0, 0);
        result.Apply();
        RenderTexture.active = currentRT;
        return result;
    }

    /// <summary>
    /// コルーチンでファイル読み込み
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    IEnumerator LoadFileCoroutine(string file)
    {
        WWW www = new WWW(file);
        yield return www;

        // 前のtextureを破棄
        Destroy(texture);

        // Quiltを読み込み
        texture = www.texture;
        holoplay.customQuiltSettings = GetTilingType(texture);
        Debug.Log("kokokara");
        Debug.Log(holoplay.customQuiltSettings.quiltWidth);
        Debug.Log(holoplay.customQuiltSettings.quiltHeight);
        Debug.Log(holoplay.customQuiltSettings.viewWidth);
        Debug.Log(holoplay.customQuiltSettings.viewHeight);
        Debug.Log(holoplay.customQuiltSettings.viewColumns);
        Debug.Log(holoplay.customQuiltSettings.viewRows);
        holoplay.overrideQuilt = texture;
        holoplay.quiltPreset = Quilt.Preset.Custom;
        holoplay.SetupQuilt();

        // 念のため毎回GCをしてみる…
        System.GC.Collect();

        // Debug.Log("Estimaged tiling: " + quilt.tiling.presetName);     // 選択されたTiling

        // 読み込み完了
        isLoading = false;

        yield return null;
    }

    [SerializeField]
    RenderTexture renderTexture;

    /// <summary>
    /// スライドショーでの次のファイルパスを返す
    /// </summary>
    /// <returns>path</returns>
    /// <param name="step">1なら１つ次、-1なら１つ前</param>
    private string GetNextFile(int step)
    {
        List<string> files;
        int currentIndex = 0;

        if (targetFiles.Count > 0)
        {
            // 対象ファイルが指定されている場合はそのリストをたどる
            currentIndex = targetFiles.IndexOf(currentFile);
            files = targetFiles;
        }
        else
        {
            // 対象ファイル指定なしならば、現在のファイルと同じディレクトリから一覧を取得
            //   利便性のため、毎回一覧を取得
            string directory = Path.GetDirectoryName(currentFile);
            string filename = Path.GetFileName(currentFile);
            files = new List<string>();

            string[] allFiles = Directory.GetFiles(directory);
            foreach (string path in allFiles)
            {
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".png" || ext == ".jpg" || ext == ".jpeg")
                {
                    files.Add(path);
                    if (Path.GetFileName(path) == filename)
                    {
                        currentFile = path;
                    }
                    Debug.Log(path);
                }
            }

            files.Sort();
            currentIndex = files.IndexOf(currentFile);
            Debug.Log("Index: " + currentIndex);
        }

        Debug.Log("Current index: " + currentIndex + "  Step: " + step);
        int index = currentIndex + step;
        if ((currentIndex >= (files.Count - 1)) && (step > 0))
        {
            // 最後のファイル表示中にさらに次を押されたら、最初に送る
            index = 0;
        }
        else if ((currentIndex == 0) && (step < 0))
        {
            // 最初のファイル表示中にさらに前を押されたら、最後に送る
            index = files.Count - 1;
        }

        if (index < 0)
        {
            // インデックスが0より小さくなったら、先頭とする
            index = 0;
        }
        else if (index >= files.Count)
        {
            // インデックスがリストを超えたら、最後に送る
            index = files.Count - 1;
        }
        return files[index];
    }

    /// <summary>
    /// ダイアログからファイルを開く
    /// </summary>
    private void OpenFile()
    {
        // Standalone File Browserを利用
        var extensions = new[] {
                new ExtensionFilter("Image Files", "png", "jpg", "jpeg" ),
                new ExtensionFilter("All Files", "*" ),
            };
        string[] files = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, false);
        if (files.Length < 1) return;

        string path = files[0];
        if (!string.IsNullOrEmpty(path))
        {
            LoadFile(path);
        }
    }

    /// <summary>
    /// ファイルがドロップされた時の処理
    /// </summary>
    /// <param name="files"></param>
    private void Window_OnFilesDropped(string[] files)
    {
        // 自分のウィンドウにフォーカスを与える
        window.Focus();

        if (files.Length == 1)
        {
            // 一つだけドロップの場合はスライドショーリストを消去
            targetFiles.Clear();

            // ファイルだけ読み込み
            LoadFile(files[0]);
        }
        else if (files.Length > 1)
        {
            // 複数のファイルがあれば、それらをスライドショー対象とする
            targetFiles.Clear();
            targetFiles.AddRange(files);
            targetFiles.Sort();

            // 最初のファイルだけ読み込み
            LoadFile(targetFiles[0]);
        }
    }

    /// <summary>
    /// 自己相関からタイル数を推定
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    private Quilt.Settings GetTilingType(Texture2D texture)
    {
        var tilingPresets = new List<Quilt.Settings>();
        foreach (var setting in Quilt.presets)
        {
            if ((setting.quiltWidth == texture.height) && (setting.quiltHeight == texture.width))
            {
                // 画像サイズがプリセットのサイズと一致すれば候補とする
                tilingPresets.Add(setting);
            }
            else
            {
                // サイズが一致しなければ、そのtileX,tileYでサイズを合わせた候補を作成
                tilingPresets.Add(
                    new Quilt.Settings(
                        texture.width, texture.height,
                        setting.viewColumns, setting.viewRows,
                        setting.viewColumns * setting.viewRows
                    ));
            }
        }

        // どれも候補に残らなければ初期指定のTilingにしておく
        if (tilingPresets.Count < 1)
        {
            return defaultSetting;
        }

        // テクスチャを配列に取得
        Color[] pixels = texture.GetPixels(0, 0, texture.width, texture.height);

        // Tiling候補ごとの自己相関を求める
        float[] score = new float[tilingPresets.Count];

        // 相関をとる周期の調整値。1だと全ピクセルについて相関をとるが遅い。
        int skip = texture.width / 512;     // 4;

        int index = 0;
        foreach (var preset in tilingPresets)
        {
            score[index] = 0;
            for (int v = 0; v < preset.viewHeight; v += skip)
            {
                for (int u = 0; u < preset.viewWidth; u += skip)
                {
                    Color sum = Color.clear;
                    for (int y = 0; y < preset.viewRows; y++)
                    {
                        for (int x = 0; x < preset.viewColumns; x++)
                        {
                            Color color = pixels[(y * preset.viewHeight + v) * texture.width + (x * preset.viewWidth + u)];
                            sum += color;
                        }
                    }
                    Color average = sum / preset.numViews;

                    Color variance = Color.clear;
                    for (int y = 0; y < preset.viewRows; y++)
                    {
                        for (int x = 0; x < preset.viewColumns; x++)
                        {
                            Color color = pixels[(y * preset.viewHeight + v) * texture.width + (x * preset.viewWidth + u)];
                            Color diff = color - average;
                            variance += diff * diff;
                        }
                    }
                    score[index] += variance.r + variance.g + variance.b;
                }
            }
            index++;
        }

        // 最も相関が高かったプリセットを選択
        int selectedIndex = 0;
        float minScore = float.MaxValue;
        for (int i = 0; i < tilingPresets.Count; i++)
        {
            //Debug.Log(tilingPresets[i].presetName + " : " + score[i]);

            if (minScore > score[i])
            {
                selectedIndex = i;
                minScore = score[i];
            }
        }
        return tilingPresets[selectedIndex];
    }
}
