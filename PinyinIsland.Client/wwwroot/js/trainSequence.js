export function scrollToWagonSlot(slotIndex) {
    try {
        const el = document.getElementById('wagon_slot_' + slotIndex);
        if (el) {
            el.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
        }
    } catch (e) { }
}

export function scrollToTrainFront() {
    try {
        const trackView = document.querySelector('.train-track-view');
        if (trackView) {
            trackView.scrollTo({ left: 0, behavior: 'smooth' });
        }
    } catch (e) { }
}
