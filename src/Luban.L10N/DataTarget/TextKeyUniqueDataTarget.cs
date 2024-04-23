using Luban.DataTarget;
using Luban.DataVisitors;
using Luban.Defs;

namespace Luban.L10N.DataTarget;

[DataTarget("text-unique-list")]
internal class TextKeyUniqueDataTarget : DataTargetBase
{
    protected override string OutputFileExt => "txt";

    public override bool ExportAllRecords => true;

    public override AggregationType AggregationType => AggregationType.Tables;

    public override OutputFile ExportTable(DefTable table, List<Record> records)
    {
        throw new NotImplementedException();
    }

    public override OutputFile ExportTables(List<DefTable> tables)
    {
        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        if (GenerationContext.Current.TextProvider != null && GenerationContext.Current.TextProvider is LocTextProvider locTextProvider)
        {
            builder.AppendLine("##var,id,cn,en");
            builder.AppendLine("##type,string,string,string");
            builder.AppendLine("##group,,,");
            builder.AppendLine("##,id,cn,en");
            var allText = locTextProvider.TextKeys;
            for (int i = 0; i < allText.Count; i++)
            {
                builder.AppendLine($",{i},{allText[i]},");
            }
                
            var content = builder.ToString();

            string outputFile = EnvManager.Current.GetOption(BuiltinOptionNames.Loc, BuiltinOptionNames.LocUniqueFilePath, false);

            return new OutputFile { File = outputFile, Content = content };
        }
        return null;
    }
}
