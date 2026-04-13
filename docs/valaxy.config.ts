import type { ThemeConfig } from 'valaxy-theme-nova'
import { defineValaxyConfig } from 'valaxy'

export default defineValaxyConfig<ThemeConfig>({
  theme: 'nova',

  siteConfig: {
    title: 'DevMode',
    url: 'https://devmode-sts2.local',
    description: 'Developer mode mod for Slay the Spire 2 — documentation',
    lang: 'en',
    languages: ['en', 'zh-CN'],

    author: {
      name: 'WRXinYue',
    },

    search: {
      enable: false,
    },
  },

  themeConfig: {
    colors: {
      primary: '#BB6516',
    },

    navTitle: { en: 'DevMode', 'zh-CN': 'DevMode' },

    nav: [
      { locale: 'nav.home', link: '/' },
      {
        locale: 'nav.guide',
        link: '/guide/install',
        subNav: [
          { locale: 'nav.install', link: '/guide/install' },
          { locale: 'nav.panels_overview', link: '/guide/panels' },
        ],
      },
      {
        locale: 'nav.extending',
        link: '/developer/extending/panel-registry',
        subNav: [
          { locale: 'nav.dev_panel', link: '/developer/extending/panel-registry' },
          { locale: 'nav.mod_runtime', link: '/developer/extending/mod-runtime' },
          {
            locale: 'nav.contributing',
            link: '/developer/dev',
          },
        ],
      },
    ],

    navTools: [['toggleLocale', 'toggleTheme']],

    hero: {
      title: { en: 'DEVMODE', 'zh-CN': 'DEVMODE' },
      motto: {
        en: 'Slay the Spire 2 developer tools & extension API',
        'zh-CN': '《杀戮尖塔 2》开发者工具与扩展接口',
      },
      img: 'https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp',
    },

    footer: {
      since: 2026,
    },
  },
})
