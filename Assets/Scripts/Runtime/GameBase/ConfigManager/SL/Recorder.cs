using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// MVP框架中所有Model的基接口
/// 定义模型层的通用生命周期和数据通知规范
/// </summary>
public interface IModel
{
    /// <summary>
    /// 模型初始化（对应Unity的Awake/Start，在Model创建时调用）
    /// </summary>
    void Init();

    /// <summary>
    /// 模型销毁（对应Unity的OnDestroy，释放资源/取消监听）
    /// </summary>
    void Dispose();

    ///// <summary>
    ///// 数据更新通知（可选，用于Model向Presenter推送数据变化）
    ///// </summary>
    //event Action<string, object> OnDataChanged;
}

/// <summary>
/// 本地存档类
/// </summary>
public class Recorder : SingletonBase<Recorder>
{
    /// <summary>
    /// 不同模式下的存储路径
    /// </summary>
    private string RecordPath
    {
        get
        {
#if (UNITY_EDITOR || UNITY_STANDALONE)
            return GlobalPath.data_RecordPath;
#else
            return GlobalPath.data_RecordPathInPackage;
#endif
        }
    }

    /// <summary>
    /// 临时存储存档的字典 暂存数据等待后续调用写入到存档中
    /// </summary>
    private Dictionary<string,string> _cache = new Dictionary<string,string>();

    public Recorder()
    {
        _cache.Clear();
        FileInfo[] files = FileUtil.LoadFiles(RecordPath);
        foreach (var f in files)
        {
            string key = Path.GetFileNameWithoutExtension(f.FullName);//不带拓展名的文件名
            string value = File.ReadAllText(f.FullName);//根据路径读取对应文件的所有内容
            _cache.Add(key, value);//初始化对应存档
        }
    }

    /// <summary>
    /// 强制保存
    /// 将cache内容存档到本地
    /// </summary>
    public void ForceSave()
    {
        FileInfo[] files = FileUtil.LoadFiles(RecordPath);
        foreach(var f in files)
        {
            string name = Path.GetFileNameWithoutExtension(f.FullName);
            if (_cache.ContainsKey(name))
            {
                string path = string.Format("{0}/{1}/{2}.record",Application.dataPath, RecordPath, name);
                if(File.Exists(path))
                    File.Delete(path);
                File.WriteAllText(path, _cache[name]);
            }
        }
    }

    /// <summary>
    /// 读取存档
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public DataList<T> LoadData<T>()
    {
        try
        {
            string fileContent = _cache[typeof(T).Name];
            DataList<T> dataList = JsonUtility.FromJson<DataList<T>>(fileContent);
            return dataList;
        }
        catch (Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }

    /// <summary>
    /// 存储数据到缓存中
    /// 非必要不使用 save = true,建议使用ForceSave进行一次性的统一存储
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">文件数据</param>
    /// <param name="save">是否保存为存档</param>
    /// <exception cref="Exception"></exception>
    public void SaveData<T>(DataList<T> data,bool save = false)
    {
        string json = JsonUtility.ToJson(data);
        json = FormatJson(json);

        try
        {
            _cache[typeof(T).Name] = json;
            if (save)
            {
                string path = string.Format("{0}/{1}/{2}.record",Application.dataPath, RecordPath, typeof(T).Name);
                if (File.Exists(path))
                    File.Delete(path);
                File.WriteAllText(path, json);
            }
        }
        catch(System.Exception ex)
        {
            throw new Exception(ex.ToString());
        }
    }

    #region  增删查改 CURD
    /// <summary>
    /// 增加数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="save"></param>
    public void CreateData<T>(T data, bool save = false)
    {
        DataList<T> dataList = LoadData<T>();
        dataList.datas.Add(data);
        SaveData<T>(dataList, save);
    }

    /// <summary>
    /// 修改数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="index"></param>
    /// <param name="data"></param>
    /// <param name="save"></param>
    /// <exception cref="System.Exception"></exception>
    public void UpdateData<T>(int index, T data, bool save = false)
    {
        try
        {
            DataList<T> dataList = LoadData<T>();
            dataList.datas[index] = data;
            SaveData<T>(dataList, save);
        }
        catch (Exception err)
        {
            throw new System.Exception(err.ToString());
        }
    }

    /// <summary>
    /// 查数据
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="index"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    //public T ReadData<T>(int index) where T : IModel
    public T ReadData<T>(int index)
    {
        try
        {
            DataList<T> dataList = LoadData<T>();
            return dataList.datas[index];
        }
        catch (Exception err)
        {
            throw new System.Exception(err.ToString());
        }

    }

    /// <summary>
    /// 删数据 按文件删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data"></param>
    /// <param name="save"></param>
    public void DeleteData<T>(T data, bool save = false)
    {
        DataList<T> dataList = LoadData<T>();
        dataList.datas.Remove(data);
        SaveData<T>(dataList, save);
    }

    /// <summary>
    /// 删数据 按索引删除
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="index"></param>
    /// <param name="save"></param>
    public void DeleteData<T>(int index, bool save = false)
    {
        try
        {
            DataList<T> dataList = LoadData<T>();
            dataList.datas.RemoveAt(index);
            SaveData<T>(dataList, save);
        }
        catch (System.Exception)
        {
            throw;
        }
    }
    #endregion

    /// <summary>
    /// 自动序列化Json文件，适配.record后缀文件
    /// </summary>
    private static string FormatJson(string compact)
    {
        if (string.IsNullOrEmpty(compact)) return compact;

        const char QUOTE = (char)34;      //双引号
        const char BACKSLASH = (char)92;  //反斜杠

        StringBuilder sb = new StringBuilder(compact.Length * 2);
        int depth = 0;//嵌套层级，1 层 = 4 个空格
        bool inString = false;

        for (int i = 0; i < compact.Length; i++)
        {
            char c = compact[i];

            if (inString)//字符串内部原样搬运，只处理转义与结束引号
            {
                sb.Append(c);
                if (c == BACKSLASH && i + 1 < compact.Length)
                    sb.Append(compact[++i]);
                else if (c == QUOTE)
                    inString = false;
                continue;
            }

            switch (c)
            {
                case QUOTE:
                    inString = true;
                    sb.Append(c);
                    break;

                case '{':
                case '[':
                    sb.Append(c);
                    //空容器 {} 或 [] 不换行，免得出现只有一对括号的两行
                    if (i + 1 < compact.Length && compact[i + 1] != '}' && compact[i + 1] != ']')
                    {
                        depth++;
                        sb.Append('\n');
                        sb.Append(' ', depth * 4);
                    }
                    break;

                case '}':
                case ']':
                    //与上面的空容器判断保持对称
                    if (depth > 0 && i > 0 && compact[i - 1] != '{' && compact[i - 1] != '[')
                    {
                        depth--;
                        sb.Append('\n');
                        sb.Append(' ', depth * 4);
                    }
                    sb.Append(c);
                    break;

                case ',':
                    sb.Append(c).Append('\n').Append(' ', depth * 4);
                    break;

                case ':':
                    sb.Append(c).Append(' ');
                    break;

                case ' '://紧凑文本里不该有空白，真有也丢掉，免得出现多余空行
                case '\t':
                case '\r':
                case '\n':
                    break;

                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }
}
