using Microsoft.EntityFrameworkCore;
using ProviEmplea.Web.Contracts.Talent;
using ProviEmplea.Web.Data;
using ProviEmplea.Web.Data.Entities;

namespace ProviEmplea.Web.Services;

public sealed class TalentDocumentService(
    ProviEmpleaDbContext db,
    IConfiguration configuration,
    IWebHostEnvironment environment)
{
    public const string ProofOfResidenceType = "comprobante_residencia";
    public const long MaximumFileSize = 5 * 1024 * 1024;

    private static readonly IReadOnlyDictionary<string, (string ContentType, byte[] Signature)> AllowedTypes =
        new Dictionary<string, (string, byte[])>(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = ("application/pdf", "%PDF-"u8.ToArray()),
            [".doc"] = ("application/msword", [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1]),
            [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", [0x50, 0x4B, 0x03, 0x04]),
            [".jpg"] = ("image/jpeg", [0xFF, 0xD8, 0xFF]),
            [".jpeg"] = ("image/jpeg", [0xFF, 0xD8, 0xFF]),
            [".png"] = ("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A])
        };

    public string StorageRoot
    {
        get
        {
            var configuredPath = configuration["FileStorage:RootPath"];
            return Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "Uploads")
                : configuredPath);
        }
    }

    public async Task<(TalentDocumentResponse? Document, string? Error)> UploadProofOfResidenceAsync(
        Guid userId,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            return (null, "Selecciona un archivo para subir.");
        }

        if (file.Length > MaximumFileSize)
        {
            return (null, "El archivo supera el límite de 5 MB.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedTypes.TryGetValue(extension, out var fileType))
        {
            return (null, "Formato no permitido. Usa PDF, Word, JPG o PNG.");
        }

        if (!string.Equals(file.ContentType, fileType.ContentType, StringComparison.OrdinalIgnoreCase))
        {
            return (null, "El tipo de contenido no coincide con la extensión del archivo.");
        }

        var profile = await db.TalentProfiles.SingleOrDefaultAsync(
            item => item.UserId == userId && item.DeletedAt == null,
            cancellationToken);
        if (profile is null)
        {
            return (null, "Completa primero los datos básicos de tu perfil.");
        }

        await using var input = file.OpenReadStream();
        var signature = new byte[fileType.Signature.Length];
        var bytesRead = await input.ReadAsync(signature, cancellationToken);
        if (bytesRead != signature.Length || !signature.SequenceEqual(fileType.Signature))
        {
            return (null, "El contenido del archivo no corresponde al formato declarado.");
        }

        input.Position = 0;
        Directory.CreateDirectory(StorageRoot);

        var documentId = Guid.NewGuid();
        var storageKey = $"{documentId:N}{extension.ToLowerInvariant()}";
        var filePath = GetSafePath(storageKey);
        var temporaryPath = $"{filePath}.tmp";
        try
        {
            await using (var output = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await input.CopyToAsync(output, cancellationToken);
                await output.FlushAsync(cancellationToken);
            }

            File.Move(temporaryPath, filePath);

            var document = new TalentDocument
            {
                Id = documentId,
                TalentProfileId = profile.Id,
                DocumentType = ProofOfResidenceType,
                Extension = extension[1..].ToLowerInvariant(),
                ContentType = fileType.ContentType,
                StorageKey = storageKey,
                SizeBytes = file.Length,
                UploadedAt = DateTime.UtcNow
            };
            db.TalentDocuments.Add(document);
            await db.SaveChangesAsync(cancellationToken);

            return (new TalentDocumentResponse(
                document.Id,
                document.DocumentType,
                document.Extension,
                document.SizeBytes,
                document.UploadedAt), null);
        }
        catch
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            throw;
        }
    }

    public async Task<StoredTalentDocument?> GetOwnedProofDocumentAsync(
        Guid userId,
        Guid? documentId,
        CancellationToken cancellationToken)
    {
        var query = db.TalentDocuments
            .AsNoTracking()
            .Where(document => document.TalentProfile.UserId == userId &&
                               document.TalentProfile.DeletedAt == null &&
                               document.DocumentType == ProofOfResidenceType &&
                               document.DeletedAt == null);
        if (documentId.HasValue)
        {
            query = query.Where(document => document.Id == documentId.Value);
        }

        var document = await query
            .OrderByDescending(item => item.UploadedAt)
            .Select(item => new StoredTalentDocument(item.StorageKey, item.Extension, item.Id))
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        var path = GetSafePath(document.StorageKey);
        return File.Exists(path) ? document with { FullPath = path } : null;
    }

    public async Task<StoredTalentDocument?> GetProofDocumentForAdminAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var documentId = await db.TalentDocuments
            .Where(document => document.TalentProfile.UserId == userId &&
                               document.TalentProfile.DeletedAt == null &&
                               document.DocumentType == ProofOfResidenceType &&
                               document.DeletedAt == null)
            .OrderByDescending(document => document.UploadedAt)
            .Select(document => (Guid?)document.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (documentId is null)
        {
            return null;
        }

        var document = await db.TalentDocuments
            .AsNoTracking()
            .Where(item => item.Id == documentId.Value)
            .Select(item => new StoredTalentDocument(item.StorageKey, item.Extension, item.Id))
            .SingleOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        var path = GetSafePath(document.StorageKey);
        return File.Exists(path) ? document with { FullPath = path } : null;
    }

    private string GetSafePath(string storageKey)
    {
        var root = StorageRoot;
        var fullPath = Path.GetFullPath(Path.Combine(root, storageKey));
        if (!fullPath.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("La clave del archivo está fuera del almacenamiento autorizado.");
        }

        return fullPath;
    }
}

public sealed record StoredTalentDocument(string StorageKey, string Extension, Guid Id)
{
    public string? FullPath { get; init; }
}
