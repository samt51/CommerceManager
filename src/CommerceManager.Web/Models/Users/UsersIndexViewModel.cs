namespace CommerceManager.Web.Models.Users;

public sealed class UsersIndexViewModel
{
    public List<UserListItemViewModel> Users { get; set; } = [];
    public List<RoleViewModel> Roles { get; set; } = [];
}
