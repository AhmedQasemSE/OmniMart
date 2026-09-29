using OmniMart.Domain.Enums;

namespace OmniMart.Domain.Entities
{
    public class StaffProfile
    {
        public Guid Id { get; private set; }
        public Guid UserId { get; private set; }
        public Department DepartmentRole { get; private set; }
        public string StaffNumber { get; private set; }
        public byte[] RowVersion { get; private set; } = null!;
        public decimal Salary { get; private set; }

        public virtual User? User { get; private set; }

        public StaffProfile(Guid userId, string staffNumber, Department departmentRole, decimal salary)  
        { 
            if (userId == Guid.Empty) throw new ArgumentException("UserId cannot be empty.", nameof(userId));
            if (salary < 0)
                throw new ArgumentException("Salary cannot be negative." , nameof(salary));
            Id = Guid.NewGuid();
            UserId = userId;
            StaffNumber = staffNumber;
            DepartmentRole = departmentRole;
            Salary = salary;
        }
#pragma warning disable CS8618
        protected StaffProfile() { }
#pragma warning restore CS8618

        public void IncreaseSalary(decimal amount) 
        {
            if (amount <= 0)
                throw new ArgumentException("Amount to increase salary must be positive.", nameof(amount));
            Salary += amount;
        }
        public void TransferDepartment(Department newDepartment ) 
        {
            if (DepartmentRole == newDepartment)
                throw new InvalidOperationException($"Staff member is already in the {newDepartment} department.");

            DepartmentRole = newDepartment;

        }
    }
}
