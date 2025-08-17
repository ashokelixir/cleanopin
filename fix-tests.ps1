# Fix SaveChangesAsync mocks in test files
$files = @(
    "tests/CleanArchTemplate.UnitTests/Application/Features/Permissions/Commands/AssignPermissionToUserCommandHandlerTests.cs",
    "tests/CleanArchTemplate.UnitTests/Application/Features/Permissions/Commands/RemovePermissionFromUserCommandHandlerTests.cs",
    "tests/CleanArchTemplate.UnitTests/Application/Features/Permissions/Commands/BulkAssignPermissionsCommandHandlerTests.cs"
)

foreach ($file in $files) {
    $content = Get-Content $file -Raw
    $content = $content -replace '_unitOfWorkMock\.Setup\(x => x\.SaveChangesAsync\(It\.IsAny<CancellationToken>\(\)\)\)\s*\.Returns\(Task\.CompletedTask\);', '_unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);'
    Set-Content $file $content
    Write-Host "Fixed $file"
}