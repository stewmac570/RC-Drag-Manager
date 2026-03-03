namespace RCDragManagerProd.Domain
{
    public static class ByePolicy
    {
        public static bool IsBye(Driver d) => d == null;
    }
}
