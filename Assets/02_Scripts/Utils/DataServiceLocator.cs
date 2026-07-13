public static class DataServiceLocator
{
    // 전역적으로 접근 가능한 ISaveRepository 인스턴스 (DataManager가 주입함)
    public static ISaveRepository SaveRepo { get; set; }
}
