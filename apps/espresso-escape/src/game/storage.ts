import AsyncStorage from "@react-native-async-storage/async-storage";

export const HIGH_SCORE_KEY = "@casa-rustico/escape-best-v1";
export const FIRST_RUN_KEY = "@casa-rustico/escape-first-run-v1";

export async function loadBestScore(): Promise<number> {
  try {
    const raw = await AsyncStorage.getItem(HIGH_SCORE_KEY);
    const n = Number.parseInt(raw ?? "", 10);
    return Number.isFinite(n) && n > 0 ? n : 0;
  } catch {
    return 0;
  }
}

export async function saveBestScore(score: number): Promise<number> {
  const prev = await loadBestScore();
  const next = Math.max(prev, score);
  try {
    await AsyncStorage.setItem(HIGH_SCORE_KEY, String(next));
    return next;
  } catch {
    return prev;
  }
}

export async function loadSeenFirstRun(): Promise<boolean> {
  try {
    return (await AsyncStorage.getItem(FIRST_RUN_KEY)) === "1";
  } catch {
    // A failed read is not "never played". That path skips the menu on every launch.
    return true;
  }
}

export async function markFirstRunSeen(): Promise<void> {
  try {
    await AsyncStorage.setItem(FIRST_RUN_KEY, "1");
  } catch {
    // Menu still works if persistence fails.
  }
}
