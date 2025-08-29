
export const formatDuration = (milliseconds: number): string => {
    const totalSeconds = Math.floor(milliseconds / 1000);
    const mins = Math.floor(totalSeconds / 60);
    const secs = totalSeconds % 60;
    const paddedSecs = secs.toString().padStart(2, '0');
    return `${mins}:${paddedSecs}`;
};