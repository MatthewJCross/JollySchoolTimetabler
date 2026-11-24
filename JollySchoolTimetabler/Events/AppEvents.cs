namespace JollySchoolTimetabler.Events
{
    public static class AppEvents
    {
        // Triggered when database is reseeded
        public static event Action DatabaseReset;
        public static event Action? DataChanged;
        public static void RaiseDataChanged() => DataChanged?.Invoke();

        public static void RaiseDatabaseReset()
        {
            DatabaseReset?.Invoke();
        }
    }
}
