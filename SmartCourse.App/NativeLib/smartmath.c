#ifdef _WIN32
__declspec(dllexport)
#endif
int CalculateBonusCredits(int credits) {
    if (credits <= 0) return 0;
    return credits + (credits / 10);
}