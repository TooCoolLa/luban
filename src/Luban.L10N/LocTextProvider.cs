using Luban.DataLoader;
using Luban.Datas;
using Luban.Defs;
using Luban.RawDefs;
using Luban.Types;
using Luban.Utils;
using System.Linq;

namespace Luban.L10N;

[TextProvider("loc")]
public class LocTextProvider : ITextProvider
{
    private static readonly NLog.Logger s_logger = NLog.LogManager.GetCurrentClassLogger();

    private string _keyFieldName;
    private string _ValueFieldName;

    private bool _convertTextKeyToValue;
    private bool value2key;
    private readonly Dictionary<string, string> _texts = new();

    private readonly HashSet<string> _unknownTextKeys = new();
    public List<string> TextKeys = new();
    private HashSet<string> valid = new();

    public void Load()
    {
         s_logger.Info ("start loc load");
        EnvManager env = EnvManager.Current;
        // _keyFieldName = env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFileKeyFieldName, false, "");
        // if (string.IsNullOrWhiteSpace(_keyFieldName))
        // {
        //     throw new Exception($"'-x {BuiltinOptionNames.L10NFamily}.{BuiltinOptionNames.L10NTextFileKeyFieldName}=xxx' missing");
        // }

        value2key = DataUtil.ParseBool(env.GetOptionOrDefault(BuiltinOptionNames.Loc, BuiltinOptionNames.LocConvertValue2Key, false, "false"));
        if (_convertTextKeyToValue)
        {
            _ValueFieldName = env.GetOptionOrDefault(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFileLanguageFieldName, false, "");
            if (string.IsNullOrWhiteSpace(_ValueFieldName))
            {
                throw new Exception($"'-x {BuiltinOptionNames.L10NFamily}.{BuiltinOptionNames.L10NTextFileLanguageFieldName}=xxx' missing");
            }
        }

        // string textProviderFile = env.GetOption(BuiltinOptionNames.L10NFamily, BuiltinOptionNames.L10NTextFilePath, false);
        // LoadTextListFromFile(textProviderFile);
        

    }
    private void AddCHeckString(ref List<string> texts,ref HashSet<string> valid,string value)
    {
        if(!valid.Contains(value))
        {
            texts.Add(value);
            valid.Add(value);
            s_logger.Info($"add key {texts.Count -1} value {value}");
        }
    }
    public bool ConvertTextKeyToValue => value2key;

    public bool IsValidKey(string key)
    {
        return _texts.ContainsKey(key);
    }

    public bool TryGetText(string value, out string key)
    {
        bool ret = valid.Contains(value);
        key = string.Empty;
        if (ret)
        {
            key = TextKeys.FindIndex(x => x.Equals(value)).ToString();
        }
        return ret;
    }

    private void LoadTextListFromFile(string fileName)
    {
        var ass = new DefAssembly(new RawAssembly()
        {
            Targets = new List<RawTarget> { new() { Name = "default", Manager = "Tables" } },
        }, "default", new List<string>());


        var rawFields = new List<RawField> { new() { Name = _keyFieldName, Type = "string" }, };
        if (_convertTextKeyToValue)
        {
            rawFields.Add(new() { Name = _ValueFieldName, Type = "string" });
        }
        var defTableRecordType = new DefBean(new RawBean()
        {
            Namespace = "__intern__",
            Name = "__TextInfo__",
            Parent = "",
            Alias = "",
            IsValueType = false,
            Sep = "",
            Fields = rawFields,
        })
        {
            Assembly = ass,
        };

        ass.AddType(defTableRecordType);
        defTableRecordType.PreCompile();
        defTableRecordType.Compile();
        defTableRecordType.PostCompile();
        var tableRecordType = TBean.Create(false, defTableRecordType, null);

        (var actualFile, var sheetName) = FileUtil.SplitFileAndSheetName(FileUtil.Standardize(fileName));
        var records = DataLoaderManager.Ins.LoadTableFile(tableRecordType, actualFile, sheetName, new Dictionary<string, string>());

        foreach (var r in records)
        {
            DBean data = r.Data;

            string key = ((DString)data.GetField(_keyFieldName)).Value;
            string value = _convertTextKeyToValue ? ((DString)data.GetField(_ValueFieldName)).Value : key;
            if (string.IsNullOrEmpty(key))
            {
                s_logger.Error("textFile:{} key:{} is empty. ignore it!", fileName, key);
                continue;
            }
            if (!_texts.TryAdd(key, value))
            {
                s_logger.Error("textFile:{} key:{} is duplicated", fileName, key);
            }
        };
    }

    public void AddUnknownKey(string key)
    {
        _unknownTextKeys.Add(key);
    }

    public void ProcessDatas()
    {
        var allTables = GenerationContext.Current.Tables;
        TextKeys.Clear();
        valid.Clear();
        s_logger.Info ("start load allTables");
        foreach (var table in allTables)
        {
            var bean = table.ValueTType.DefBean;
            s_logger.Info($"now check {bean}");
            for (int index = 0; index < bean.Fields.Count; index++)
            {
                var field = bean.Fields[index];
                s_logger.Info($"now check {bean}. {field}");
                if(field.CType.HasTag("text"))
                {
                    s_logger.Info($"now check {bean}. {field} has tag");
                    var records = GenerationContext.Current.GetTableAllDataList(table);
                    s_logger.Info($"get {bean}. all datas");
                    foreach (var record in records)
                    {
                        var data = record.Data.Fields[index];

                        if(data is DString s)
                        {
                            AddCHeckString(ref TextKeys,ref valid,s.Value);
                        }
                    }
                }
            }
        }
        s_logger.Info("end loc load");
        if (value2key)
        {
            var trans = new TextKeyToValueTransformer(this);
            foreach (var table in GenerationContext.Current.Tables)
            {
                foreach (var record in GenerationContext.Current.GetTableAllDataList(table))
                {
                    record.Data = (DBean)record.Data.Apply(trans,table.ValueTType);
                }
            }
        }
    }
}
