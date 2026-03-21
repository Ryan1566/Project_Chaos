using System;	

//TestTable的Model类型
public class TestTableModel: IModel
{
	private uint _index;
	public string _charName;
	public uint _occupation;

	public uint Index => _index;//序号
	public string CharName => _charName;//角色名称
	public uint Occupation => _occupation;//职业

    public void Init()
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

