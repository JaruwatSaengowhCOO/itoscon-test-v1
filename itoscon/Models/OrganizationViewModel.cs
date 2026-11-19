namespace itoscon.Models
{
    public class OrganizationViewModel
    {
        public List<Division> Divisions { get; set; } = new();
        public List<Department> Departments { get; set; } = new();
        public List<Role> Roles { get; set; } = new();
        public List<SubRole> SubRoles { get; set; } = new();
    }
}
