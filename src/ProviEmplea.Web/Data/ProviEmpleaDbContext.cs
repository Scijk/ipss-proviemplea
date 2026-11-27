using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ProviEmplea.Web.Data.Entities;

namespace ProviEmplea.Web.Data;

public sealed class ProviEmpleaDbContext(DbContextOptions<ProviEmpleaDbContext> options)
    : DbContext(options)
{
    private static readonly ValueConverter<Guid, string> GuidStringConverter = new(
        value => value.ToString("D"),
        value => Guid.Parse(value));

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanyUser> CompanyUsers => Set<CompanyUser>();
    public DbSet<Industry> Industries => Set<Industry>();
    public DbSet<CompanyType> CompanyTypes => Set<CompanyType>();
    public DbSet<TalentProfile> TalentProfiles => Set<TalentProfile>();
    public DbSet<WorkExperience> WorkExperiences => Set<WorkExperience>();
    public DbSet<TalentDocument> TalentDocuments => Set<TalentDocument>();
    public DbSet<RecruitmentStatus> RecruitmentStatuses => Set<RecruitmentStatus>();
    public DbSet<RecruitmentRequest> RecruitmentRequests => Set<RecruitmentRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("ROLES");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("ID_ROL").ValueGeneratedNever();
            entity.Property(role => role.Name).HasColumnName("NOMBRE").HasMaxLength(50).IsRequired();
            entity.HasIndex(role => role.Name).IsUnique();
            entity.HasData(
                new Role { Id = 1, Name = "admin" },
                new Role { Id = 2, Name = "talento" },
                new Role { Id = 3, Name = "empresa" });
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("USUARIOS");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id)
                .HasColumnName("ID_USUARIO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(user => user.RoleId).HasColumnName("ID_ROL");
            entity.Property(user => user.Email).HasColumnName("CORREO").HasMaxLength(255).IsRequired();
            entity.Property(user => user.PasswordHash).HasColumnName("PASSWORD_HASH").HasMaxLength(255).IsRequired();
            entity.Property(user => user.ValidationStatus).HasColumnName("ESTADO_VALIDACION").HasMaxLength(50).IsRequired();
            entity.Property(user => user.CreatedAt).HasColumnName("FECHA_CREACION").HasColumnType("TIMESTAMP(6)");
            entity.Property(user => user.UpdatedAt).HasColumnName("FECHA_ACTUALIZACION").HasColumnType("TIMESTAMP(6)");
            entity.Property(user => user.DeletedAt).HasColumnName("FECHA_ELIMINACION").HasColumnType("TIMESTAMP(6)");
            entity.HasIndex(user => user.Email).IsUnique();
            entity.HasOne(user => user.Role)
                .WithMany(role => role.Users)
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Industry>(entity =>
        {
            entity.ToTable("RUBROS_EMPRESA");
            entity.HasKey(industry => industry.Id);
            entity.Property(industry => industry.Id).HasColumnName("ID_RUBRO").ValueGeneratedNever();
            entity.Property(industry => industry.Name).HasColumnName("NOMBRE").HasMaxLength(100).IsRequired();
            entity.HasIndex(industry => industry.Name).IsUnique();
            entity.HasData(
                new Industry { Id = 1, Name = "Tecnología" },
                new Industry { Id = 2, Name = "Salud" },
                new Industry { Id = 3, Name = "Educación" },
                new Industry { Id = 4, Name = "Comercio" },
                new Industry { Id = 5, Name = "Construcción" },
                new Industry { Id = 6, Name = "Gastronomía" },
                new Industry { Id = 7, Name = "Finanzas" },
                new Industry { Id = 8, Name = "Legal" },
                new Industry { Id = 9, Name = "Marketing" },
                new Industry { Id = 10, Name = "Logística" });
        });

        modelBuilder.Entity<CompanyType>(entity =>
        {
            entity.ToTable("TIPOS_EMPRESA");
            entity.HasKey(type => type.Id);
            entity.Property(type => type.Id).HasColumnName("ID_TIPO").ValueGeneratedNever();
            entity.Property(type => type.Name).HasColumnName("NOMBRE").HasMaxLength(100).IsRequired();
            entity.HasIndex(type => type.Name).IsUnique();
            entity.HasData(
                new CompanyType { Id = 1, Name = "Contratación directa" },
                new CompanyType { Id = 2, Name = "EST" },
                new CompanyType { Id = 3, Name = "Outsourcing" });
        });

        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("EMPRESAS");
            entity.HasKey(company => company.Id);
            entity.Property(company => company.Id)
                .HasColumnName("ID_EMPRESA")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(company => company.TaxId).HasColumnName("RUT_EMPRESA").HasMaxLength(20).IsRequired();
            entity.Property(company => company.Name).HasColumnName("NOMBRE_EMPRESA").HasMaxLength(255).IsRequired();
            entity.Property(company => company.IndustryId).HasColumnName("ID_RUBRO");
            entity.Property(company => company.CompanyTypeId).HasColumnName("ID_TIPO_EMPRESA");
            entity.Property(company => company.Presentation).HasColumnName("PRESENTACION").HasColumnType("CLOB");
            entity.Property(company => company.Benefits).HasColumnName("BENEFICIOS").HasColumnType("CLOB");
            entity.Property(company => company.LogoUrl).HasColumnName("LOGO_URL").HasMaxLength(255);
            entity.Property(company => company.CreatedAt).HasColumnName("FECHA_CREACION").HasColumnType("TIMESTAMP(6)");
            entity.Property(company => company.UpdatedAt).HasColumnName("FECHA_ACTUALIZACION").HasColumnType("TIMESTAMP(6)");
            entity.Property(company => company.DeletedAt).HasColumnName("FECHA_ELIMINACION").HasColumnType("TIMESTAMP(6)");
            entity.HasIndex(company => company.TaxId).IsUnique();
            entity.HasOne<Industry>().WithMany().HasForeignKey(company => company.IndustryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CompanyType>().WithMany().HasForeignKey(company => company.CompanyTypeId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CompanyUser>(entity =>
        {
            entity.ToTable("USUARIOS_EMPRESA");
            entity.HasKey(membership => membership.UserId);
            entity.Property(membership => membership.UserId)
                .HasColumnName("ID_USUARIO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(membership => membership.CompanyId)
                .HasColumnName("ID_EMPRESA")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(membership => membership.FullName).HasColumnName("NOMBRE_COMPLETO").HasMaxLength(255).IsRequired();
            entity.Property(membership => membership.ContactPhone).HasColumnName("TELEFONO_CONTACTO").HasMaxLength(20);
            entity.HasOne(membership => membership.User)
                .WithOne(user => user.CompanyMembership)
                .HasForeignKey<CompanyUser>(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(membership => membership.Company)
                .WithMany(company => company.Users)
                .HasForeignKey(membership => membership.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TalentProfile>(entity =>
        {
            entity.ToTable("TALENTOS");
            entity.HasKey(profile => profile.Id);
            entity.Property(profile => profile.Id)
                .HasColumnName("ID_TALENTO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(profile => profile.UserId)
                .HasColumnName("ID_USUARIO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(profile => profile.FullName).HasColumnName("NOMBRE_COMPLETO").HasMaxLength(200).IsRequired();
            entity.Property(profile => profile.NationalId).HasColumnName("RUT").HasMaxLength(12).IsRequired();
            entity.Property(profile => profile.Phone).HasColumnName("TELEFONO").HasMaxLength(20).IsRequired();
            entity.Property(profile => profile.Summary).HasColumnName("RESUMEN").HasMaxLength(2000);
            entity.Property(profile => profile.CreatedAt).HasColumnName("FECHA_CREACION").HasColumnType("TIMESTAMP(6)");
            entity.Property(profile => profile.UpdatedAt).HasColumnName("FECHA_ACTUALIZACION").HasColumnType("TIMESTAMP(6)");
            entity.Property(profile => profile.DeletedAt).HasColumnName("FECHA_ELIMINACION").HasColumnType("TIMESTAMP(6)");
            entity.HasIndex(profile => profile.UserId).IsUnique();
            entity.HasIndex(profile => profile.NationalId).IsUnique();
            entity.HasOne(profile => profile.User)
                .WithOne(user => user.TalentProfile)
                .HasForeignKey<TalentProfile>(profile => profile.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WorkExperience>(entity =>
        {
            entity.ToTable("ANTECEDENTES_LABORALES");
            entity.HasKey(experience => experience.Id);
            entity.Property(experience => experience.Id)
                .HasColumnName("ID_LABORAL")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(experience => experience.TalentProfileId)
                .HasColumnName("ID_TALENTO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(experience => experience.EmployerName).HasColumnName("EMPRESA").HasMaxLength(150).IsRequired();
            entity.Property(experience => experience.JobTitle).HasColumnName("CARGO").HasMaxLength(100).IsRequired();
            entity.Property(experience => experience.Description).HasColumnName("DESCRIPCION").HasColumnType("CLOB");
            entity.Property(experience => experience.StartDate).HasColumnName("FECHA_INICIO").HasColumnType("DATE");
            entity.Property(experience => experience.EndDate).HasColumnName("FECHA_FIN").HasColumnType("DATE");
            entity.Property(experience => experience.DeletedAt).HasColumnName("FECHA_ELIMINACION").HasColumnType("TIMESTAMP(6)");
            entity.HasOne(experience => experience.TalentProfile)
                .WithMany(profile => profile.WorkExperiences)
                .HasForeignKey(experience => experience.TalentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TalentDocument>(entity =>
        {
            entity.ToTable("ARCHIVOS_TALENTO");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id)
                .HasColumnName("ID_ARCHIVO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(document => document.TalentProfileId)
                .HasColumnName("ID_TALENTO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(document => document.DocumentType).HasColumnName("TIPO_DOCUMENTO").HasMaxLength(50).IsRequired();
            entity.Property(document => document.Extension).HasColumnName("FORMATO").HasMaxLength(10).IsRequired();
            entity.Property(document => document.ContentType).HasColumnName("TIPO_CONTENIDO").HasMaxLength(100).IsRequired();
            entity.Property(document => document.StorageKey).HasColumnName("CLAVE_ALMACENAMIENTO").HasMaxLength(80).IsRequired();
            entity.Property(document => document.SizeBytes).HasColumnName("TAMANO_BYTES").IsRequired();
            entity.Property(document => document.UploadedAt).HasColumnName("FECHA_SUBIDA").HasColumnType("TIMESTAMP(6)");
            entity.Property(document => document.DeletedAt).HasColumnName("FECHA_ELIMINACION").HasColumnType("TIMESTAMP(6)");
            entity.HasIndex(document => new { document.TalentProfileId, document.DocumentType, document.UploadedAt });
            entity.HasOne(document => document.TalentProfile)
                .WithMany(profile => profile.Documents)
                .HasForeignKey(document => document.TalentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RecruitmentStatus>(entity =>
        {
            entity.ToTable("ESTADOS_SEGUIMIENTO");
            entity.HasKey(status => status.Id);
            entity.Property(status => status.Id).HasColumnName("ID_ESTADO").ValueGeneratedNever();
            entity.Property(status => status.Name).HasColumnName("NOMBRE").HasMaxLength(50).IsRequired();
            entity.HasIndex(status => status.Name).IsUnique();
            entity.HasData(
                new RecruitmentStatus { Id = 1, Name = "Solicitado" },
                new RecruitmentStatus { Id = 2, Name = "Contactado" },
                new RecruitmentStatus { Id = 3, Name = "Entrevista" },
                new RecruitmentStatus { Id = 4, Name = "Seleccionado" },
                new RecruitmentStatus { Id = 5, Name = "No seleccionado" },
                new RecruitmentStatus { Id = 6, Name = "Cerrado" });
        });

        modelBuilder.Entity<RecruitmentRequest>(entity =>
        {
            entity.ToTable("SOLICITUDES_TALENTO");
            entity.HasKey(request => request.Id);
            entity.Property(request => request.Id)
                .HasColumnName("ID_SOLICITUD")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(request => request.CompanyId)
                .HasColumnName("ID_EMPRESA")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(request => request.TalentProfileId)
                .HasColumnName("ID_TALENTO")
                .HasConversion(GuidStringConverter)
                .HasColumnType("VARCHAR2(36 CHAR)");
            entity.Property(request => request.StatusId).HasColumnName("ID_ESTADO");
            entity.Property(request => request.InternalNotes).HasColumnName("NOTAS_INTERNAS").HasColumnType("CLOB");
            entity.Property(request => request.CreatedAt).HasColumnName("FECHA_SOLICITUD").HasColumnType("TIMESTAMP(6)");
            entity.Property(request => request.UpdatedAt).HasColumnName("FECHA_ACTUALIZACION").HasColumnType("TIMESTAMP(6)");
            entity.HasIndex(request => new { request.CompanyId, request.TalentProfileId });
            entity.HasOne(request => request.Company)
                .WithMany(company => company.RecruitmentRequests)
                .HasForeignKey(request => request.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(request => request.TalentProfile)
                .WithMany()
                .HasForeignKey(request => request.TalentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(request => request.Status)
                .WithMany(status => status.Requests)
                .HasForeignKey(request => request.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<User>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<Company>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<TalentProfile>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }

        foreach (var entry in ChangeTracker.Entries<RecruitmentRequest>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
            }
        }
    }
}
