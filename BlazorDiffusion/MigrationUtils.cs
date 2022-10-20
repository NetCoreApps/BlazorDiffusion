namespace BlazorDiffusion;

public static class MigrationUtils
{
    public static T BySystemUser<T>(this T row, DateTime? date = null) where T : AuditBase
    {
        var useDate = date ?? DateTime.Now;
        row.CreatedBy = "2";
        row.CreatedDate = useDate;
        row.ModifiedBy = "2";
        row.ModifiedDate = useDate;
        return row;
    }
}