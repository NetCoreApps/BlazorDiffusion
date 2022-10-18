namespace BlazorDiffusion;

public static class MigrationUtils
{
    public static T BySystemUser<T>(this T row, DateTime? date = null) where T : AuditBase
    {
        var useDate = date ?? DateTime.Now;
        row.CreatedBy = "1";
        row.CreatedDate = useDate;
        row.ModifiedBy = "1";
        row.ModifiedDate = useDate;
        return row;
    }
}