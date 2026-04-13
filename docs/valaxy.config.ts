import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { resolve } from 'node:path'
import type { ThemeConfig } from 'valaxy-theme-nova'
import { defineValaxyConfig } from 'valaxy'
import modManifest from '../DevMode.json'

const __dirname = fileURLToPath(new URL('.', import.meta.url))

/** Vite plugin: sync root CHANGELOG files into docs/pages/ with frontmatter. */
function changelogSync() {
  const rootDir = resolve(__dirname, '..')
  const pagesDir = resolve(__dirname, 'pages')

  const entries = [
    { src: resolve(rootDir, 'CHANGELOG.md'), dest: resolve(pagesDir, 'changelog.md') },
    { src: resolve(rootDir, 'CHANGELOG.zh-CN.md'), dest: resolve(pagesDir, 'changelog-zh-cn.md') },
  ]

  const frontmatter = [
    '---',
    'title:',
    '  en: Changelog',
    '  zh-CN: 更新日志',
    'top: 9000',
    'cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp',
    '---',
    '',
    '',
  ].join('\n')

  function sync() {
    for (const { src, dest } of entries) {
      if (!existsSync(src)) continue
      writeFileSync(dest, frontmatter + readFileSync(src, 'utf-8'))
    }
  }

  return {
    name: 'changelog-sync',
    buildStart() { sync() },
    configureServer(server: any) {
      const watched = entries.map(e => e.src).filter(existsSync)
      server.watcher.add(watched)
      server.watcher.on('change', (path: string) => {
        if (watched.includes(path)) sync()
      })
    },
  }
}

export default defineValaxyConfig<ThemeConfig>({
  theme: 'nova',

  vite: {
    plugins: [changelogSync()],
  },

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
        link: '/guide/preface',
        subNav: [
          { locale: 'nav.intro', link: '/guide/preface' },
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
      {
        locale: 'nav.notes',
        link: '/notes',
        subNav: [
          { locale: 'nav.notes_hub', link: '/notes/' },
          { locale: 'nav.notes_harmony', link: '/notes/sts2-harmony-basics' },
          { locale: 'nav.notes_card_api', link: '/notes/sts2-card-api' },
          { locale: 'nav.notes_localization', link: '/notes/sts2-localization' },
          { locale: 'nav.notes_images', link: '/notes/sts2-image-standards' },
          { locale: 'nav.notes_pitfalls', link: '/notes/sts2-modding-pitfalls' },
          { locale: 'nav.notes_combat_ui', link: '/notes/sts2-combat-ui' },
          { locale: 'nav.notes_skill_tree', link: '/notes/sts2-skill-tree' },
          { locale: 'nav.notes_pets', link: '/notes/sts2-pet-guide' },
          { locale: 'nav.notes_summon', link: '/notes/sts2-summon-guide' },
          { locale: 'nav.notes_mp_sync', link: '/notes/sts2-multiplayer-sync' },
        ],
      },
      {
        locale: 'nav.changelog',
        link: '/changelog',
        subNav: [
          { text: 'English', link: '/changelog' },
          { text: '中文', link: '/changelog-zh-cn' },
        ],
      },
      {
        text: `v${modManifest.version}`,
        link: 'https://github.com/WRXinYue/STS2-DevMode/releases',
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
