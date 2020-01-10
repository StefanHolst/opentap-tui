module.exports = {
    title: 'OpenTAP TUI',
    description: 'Textual User Interface for OpenTAP',
    themeConfig: {
        nav: [
            { text: 'GitLab', link: 'https://gitlab.com/StefanHolst0/opentap-tui' },
            { text: 'OpenTAP', link: 'https://gitlab.com/opentap/opentap' },
            { text: 'OpenTAP Homepage', link: 'https://www.opentap.io' }
        ],
        sidebar: [
            ['/', 'OpenTAP TUI'],
            {
                title: 'Getting Started',
                children: [
                    '/Installing.md',
                    '/Debugging.md',
                ]
            },
            '/Navigation.md',
            'KnownIssues.md'
        ]
    },
    dest: '../public',
    base: '/opentap-tui/'

}