using CodexPet.Install;

if (args.Length != 4 || args[0] is not ("check" or "register" or "remove"))
{
    Console.Error.WriteLine("Usage: CodexPet.Install <check|register|remove> <hooks.json> <hook.exe> <ownership.json>");
    return 2;
}

try
{
    HookRegistration.Update(args[0], args[1], args[2], args[3]);
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"CodexPet 훅 설정을 변경하지 못했습니다. 설정: {args[1]}\n소유 기록: {args[3]}\n{ex.Message}\n파일을 확인·복구한 후 다시 시도해 주세요.");
    return 1;
}
