const storageKey = "controle-estoque.delivery-schedule";
const couriersStorageKey = "controle-estoque.delivery-couriers";
const meetingsStorageKey = "controle-estoque.agenda-meetings";

export function load() {
    const stored = localStorage.getItem(storageKey);
    if (!stored) {
        return [];
    }

    return JSON.parse(stored).map(shift => {
        if (shift.turno || shift.Turno) {
            return shift;
        }

        const legacyTime = shift.time ?? shift.Time ?? "";
        const legacyHour = Number.parseInt(legacyTime.split(":")[0], 10);
        const turno = Number.isFinite(legacyHour) && (legacyHour < 6 || legacyHour >= 18)
            ? "Noite"
            : "Dia";

        return { ...shift, turno };
    });
}

export function save(shifts) {
    localStorage.setItem(storageKey, JSON.stringify(shifts));
}

export function loadCouriers() {
    const stored = localStorage.getItem(couriersStorageKey);
    return stored ? JSON.parse(stored) : [];
}

export function saveCouriers(couriers) {
    localStorage.setItem(couriersStorageKey, JSON.stringify(couriers));
}

export function loadMeetings() {
    const stored = localStorage.getItem(meetingsStorageKey);
    return stored ? JSON.parse(stored) : [];
}

export function saveMeetings(meetings) {
    localStorage.setItem(meetingsStorageKey, JSON.stringify(meetings));
}
