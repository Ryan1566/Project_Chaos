using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// MVP框架中所有Model的基接口
/// 定义模型层的通用生命周期和数据通知规范
/// </summary>
public interface IModel
{
    ///// <summary>
    ///// 模型初始化（对应Unity的Awake/Start，在Model创建时调用）
    ///// </summary>
    //void Init();

    ///// <summary>
    ///// 模型销毁（对应Unity的OnDestroy，释放资源/取消监听）
    ///// </summary>
    //void Dispose();

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
    public DataList<T> LoadData<T>() where T : IModel
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
    public void SaveData<T>(DataList<T> data,bool save = false) where T : IModel
    {
        string json = JsonUtility.ToJson(data);
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
    public void CreateData<T>(T data, bool save = false) where T : IModel
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
    public void UpdateData<T>(int index, T data, bool save = false) where T : IModel
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
    public T ReadData<T>(int index) where T : IModel
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
    public void DeleteData<T>(T data, bool save = false) where T : IModel
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
    public void DeleteData<T>(int index, bool save = false) where T : IModel
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
}
