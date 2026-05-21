namespace TdpTrans.Models
{
    public class RolePermission
    {
        public int RoleId { get; set; }
        public AppRole Role { get; set; } = null!;

        public int PermissionId { get; set; }
        public AppPermission Permission { get; set; } = null!;
    }
}
