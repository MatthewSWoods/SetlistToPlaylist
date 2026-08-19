window.authHelpers = {
    getTransferToken: () => new URLSearchParams(window.location.search).get('at'),
    clearTransferToken: () => {
        const url = new URL(window.location.href);
        url.searchParams.delete('at');
        history.replaceState(null, '', url.pathname + (url.search && url.search !== '?' ? url.search : ''));
    },
    getClientKey: () => sessionStorage.getItem('clientKey'),
    setClientKey: (key) => sessionStorage.setItem('clientKey', key),
    clearClientKey: () => sessionStorage.removeItem('clientKey')
};
