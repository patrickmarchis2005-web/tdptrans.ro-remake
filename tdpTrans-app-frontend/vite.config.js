import fs from 'node:fs'
import net from 'node:net'
import os from 'node:os'
import path from 'node:path'
import { spawnSync } from 'node:child_process'
import { defineConfig, loadEnv } from 'vite'
import { configDefaults } from 'vitest/config'
import react from '@vitejs/plugin-react'

const DEFAULT_API_ORIGIN = 'https://localhost:7092'
const DEFAULT_VMWARE_BACKEND_PORT = 7092
const VMWARE_LEASES_PATH = 'C:\\ProgramData\\VMware\\vmnetdhcp.leases'
const VMWARE_RUN_PATH = 'C:\\Program Files (x86)\\VMware\\VMware Workstation\\vmrun.exe'
const LAN_CERTIFICATE_DIRECTORY = path.resolve(__dirname, 'certificates', 'lan-https')
const LAN_PFX_PATH = path.join(LAN_CERTIFICATE_DIRECTORY, 'tdptrans-frontend-lan.pfx')
const LAN_PFX_PASSPHRASE_PATH = path.join(LAN_CERTIFICATE_DIRECTORY, 'tdptrans-frontend-lan.passphrase.txt')
const LAN_HOSTS_METADATA_PATH = path.join(LAN_CERTIFICATE_DIRECTORY, 'tdptrans-frontend-lan-hosts.json')
const LAN_SETUP_SCRIPT_PATH = path.resolve(__dirname, 'scripts', 'setup-lan-https.ps1')

const normalizeOrigin = (value) => value?.trim()?.replace(/\/$/, '') ?? ''
const isTruthyEnvValue = (value) => /^(1|true|yes)$/i.test(value ?? '')
const normalizeOptionalPath = (value) => {
  const trimmedValue = value?.trim() ?? ''
  return trimmedValue ? path.resolve(trimmedValue) : ''
}

const readJsonIfPresent = (filePath) => {
  if (!fs.existsSync(filePath)) {
    return null
  }

  return JSON.parse(fs.readFileSync(filePath, 'utf8'))
}

const canReachOrigin = async (origin, timeoutMs = 750) => {
  try {
    const url = new URL(origin)
    const port = Number.parseInt(url.port || (url.protocol === 'https:' ? '443' : '80'), 10)

    return await new Promise((resolve) => {
      const socket = net.createConnection({
        host: url.hostname,
        port,
      })

      const finalize = (value) => {
        socket.removeAllListeners()
        socket.destroy()
        resolve(value)
      }

      socket.setTimeout(timeoutMs)
      socket.once('connect', () => finalize(true))
      socket.once('timeout', () => finalize(false))
      socket.once('error', () => finalize(false))
    })
  } catch {
    return false
  }
}

const getCurrentLanIpAddresses = () => {
  const interfaces = os.networkInterfaces()
  const values = new Set(['127.0.0.1'])

  for (const addresses of Object.values(interfaces)) {
    for (const address of addresses ?? []) {
      if (address.internal || address.family !== 'IPv4') {
        continue
      }

      values.add(address.address)
    }
  }

  return [...values]
}

const shouldRefreshLanCertificate = (hostsMetadata) => {
  if (!hostsMetadata) {
    return true
  }

  const currentIpAddresses = new Set(getCurrentLanIpAddresses())
  const knownLanIpAddresses = (hostsMetadata.ipAddresses ?? []).filter((ipAddress) => ipAddress !== '127.0.0.1')

  if (knownLanIpAddresses.length === 0) {
    return true
  }

  return !knownLanIpAddresses.some((ipAddress) => currentIpAddresses.has(ipAddress))
}

const regenerateLanHttpsBundle = () => {
  const result = spawnSync(
    'powershell',
    ['-ExecutionPolicy', 'Bypass', '-File', LAN_SETUP_SCRIPT_PATH],
    { encoding: 'utf8' },
  )

  if (result.status !== 0) {
    throw new Error(
      [
        'Regenerarea automata a certificatului HTTPS pentru LAN a esuat.',
        result.stderr?.trim() || result.stdout?.trim() || 'Ruleaza manual `npm run setup:https-lan`.',
      ].join(' '),
    )
  }
}

const getLanHttpsOptions = () => {
  if (!fs.existsSync(LAN_PFX_PATH) || !fs.existsSync(LAN_PFX_PASSPHRASE_PATH)) {
    regenerateLanHttpsBundle()
  }

  const hostsMetadata = readJsonIfPresent(LAN_HOSTS_METADATA_PATH)
  if (process.platform === 'win32' && shouldRefreshLanCertificate(hostsMetadata)) {
    regenerateLanHttpsBundle()
  }

  const refreshedHostsMetadata = readJsonIfPresent(LAN_HOSTS_METADATA_PATH)

  return {
    hostsMetadata: refreshedHostsMetadata,
    httpsOptions: {
      pfx: fs.readFileSync(LAN_PFX_PATH),
      passphrase: fs.readFileSync(LAN_PFX_PASSPHRASE_PATH, 'utf8').trim(),
    },
  }
}

const findUbuntuVmMacAddress = (rootDirectory) => {
  if (!rootDirectory || !fs.existsSync(rootDirectory)) {
    return ''
  }

  const pendingDirectories = [rootDirectory]

  while (pendingDirectories.length > 0) {
    const currentDirectory = pendingDirectories.pop()
    const entries = fs.readdirSync(currentDirectory, { withFileTypes: true })

    for (const entry of entries) {
      const entryPath = path.join(currentDirectory, entry.name)

      if (entry.isDirectory()) {
        pendingDirectories.push(entryPath)
        continue
      }

      if (!entry.isFile() || path.extname(entry.name).toLowerCase() !== '.vmx') {
        continue
      }

      const fileContent = fs.readFileSync(entryPath, 'utf8')
      if (!/guestOS\s*=\s*"ubuntu/i.test(fileContent)) {
        continue
      }

      const macAddressMatch = fileContent.match(/ethernet0\.generatedAddress\s*=\s*"([^"]+)"/i)
      if (macAddressMatch) {
        return macAddressMatch[1].toLowerCase()
      }
    }
  }

  return ''
}

const findUbuntuVmConfigPath = (rootDirectory) => {
  if (!rootDirectory || !fs.existsSync(rootDirectory)) {
    return ''
  }

  const pendingDirectories = [rootDirectory]

  while (pendingDirectories.length > 0) {
    const currentDirectory = pendingDirectories.pop()
    const entries = fs.readdirSync(currentDirectory, { withFileTypes: true })

    for (const entry of entries) {
      const entryPath = path.join(currentDirectory, entry.name)

      if (entry.isDirectory()) {
        pendingDirectories.push(entryPath)
        continue
      }

      if (!entry.isFile() || path.extname(entry.name).toLowerCase() !== '.vmx') {
        continue
      }

      const fileContent = fs.readFileSync(entryPath, 'utf8')
      if (/guestOS\s*=\s*"ubuntu/i.test(fileContent)) {
        return entryPath
      }
    }
  }

  return ''
}

const findVmwareLeaseIp = (macAddress) => {
  if (!macAddress || !fs.existsSync(VMWARE_LEASES_PATH)) {
    return ''
  }

  const leasesContent = fs.readFileSync(VMWARE_LEASES_PATH, 'utf8')
  const leasePattern = /lease\s+(\d+\.\d+\.\d+\.\d+)\s+\{[\s\S]*?hardware ethernet\s+([0-9a-f:]+);[\s\S]*?\}/gi
  let resolvedIpAddress = ''
  let currentMatch = leasePattern.exec(leasesContent)

  while (currentMatch) {
    if (currentMatch[2].toLowerCase() === macAddress) {
      resolvedIpAddress = currentMatch[1]
    }

    currentMatch = leasePattern.exec(leasesContent)
  }

  return resolvedIpAddress
}

const findVmwareGuestIp = (vmxPath) => {
  if (!vmxPath || !fs.existsSync(VMWARE_RUN_PATH)) {
    return ''
  }

  const result = spawnSync(
    VMWARE_RUN_PATH,
    ['-T', 'ws', 'getGuestIPAddress', vmxPath],
    { encoding: 'utf8' },
  )

  if (result.status !== 0) {
    return ''
  }

  return result.stdout.trim()
}

const resolveVmwareApiOrigin = (env) => {
  if (process.platform !== 'win32') {
    return ''
  }

  const configuredVmxPath = normalizeOptionalPath(env.VITE_VMWARE_VMX_PATH)
  const virtualMachinesDirectory = configuredVmxPath
    ? ''
    : process.env.USERPROFILE
      ? path.join(process.env.USERPROFILE, 'Documents', 'Virtual Machines')
      : ''
  const vmxPath = configuredVmxPath || findUbuntuVmConfigPath(virtualMachinesDirectory)
  const vmwareBackendPort = Number.parseInt(env.VITE_VMWARE_BACKEND_PORT ?? '', 10)
  const backendPort = Number.isInteger(vmwareBackendPort) && vmwareBackendPort > 0
    ? vmwareBackendPort
    : DEFAULT_VMWARE_BACKEND_PORT
  const liveGuestIpAddress = findVmwareGuestIp(vmxPath)

  if (liveGuestIpAddress) {
    return `https://${liveGuestIpAddress}:${backendPort}`
  }

  if (!virtualMachinesDirectory) {
    return ''
  }

  const macAddress = findUbuntuVmMacAddress(virtualMachinesDirectory)
  const ipAddress = findVmwareLeaseIp(macAddress)

  if (!ipAddress) {
    return ''
  }

  return `https://${ipAddress}:${backendPort}`
}

const resolveApiOrigin = async (env) => {
  const configuredOrigin = normalizeOrigin(env.VITE_API_ORIGIN)
  if (configuredOrigin) {
    return {
      apiOrigin: configuredOrigin,
      source: 'configured',
      warning: '',
    }
  }

  const localBackendReachable = await canReachOrigin(DEFAULT_API_ORIGIN)
  if (localBackendReachable) {
    return {
      apiOrigin: DEFAULT_API_ORIGIN,
      source: 'local',
      warning: '',
    }
  }

  if (isTruthyEnvValue(env.VITE_USE_VMWARE_API)) {
    const vmwareOrigin = resolveVmwareApiOrigin(env)
    if (vmwareOrigin && await canReachOrigin(vmwareOrigin)) {
      return {
        apiOrigin: vmwareOrigin,
        source: 'vmware',
        warning: '',
      }
    }

    return {
      apiOrigin: DEFAULT_API_ORIGIN,
      source: 'default',
      warning: 'VMware backend auto-detection was requested, but no reachable Ubuntu guest backend was found. The frontend is falling back to https://localhost:7092.',
    }
  }

  const vmwareOrigin = resolveVmwareApiOrigin(env)
  if (vmwareOrigin && await canReachOrigin(vmwareOrigin)) {
    return {
      apiOrigin: vmwareOrigin,
      source: 'vmware-fallback',
      warning: 'Backend-ul local de pe https://localhost:7092 nu raspunde. Frontend-ul foloseste automat backend-ul detectat in VMware.',
    }
  }

  return {
    apiOrigin: DEFAULT_API_ORIGIN,
    source: 'default',
    warning: 'Nu a fost detectat niciun backend activ pe https://localhost:7092 si niciun guest Ubuntu pornit in VMware. Porneste backend-ul local sau masina virtuala Ubuntu.',
  }
}

// https://vite.dev/config/
export default defineConfig(async ({ command, mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const { apiOrigin, source: apiOriginSource, warning: apiOriginWarning } = await resolveApiOrigin(env)
  const chatOrigin = apiOrigin.replace(/^http/i, 'ws')
  const lanHttpsBundle = command === 'build' ? null : getLanHttpsOptions()
  const httpsOptions = lanHttpsBundle?.httpsOptions
  const httpsHostsLabel = lanHttpsBundle?.hostsMetadata
    ? [...(lanHttpsBundle.hostsMetadata.dnsNames ?? []), ...(lanHttpsBundle.hostsMetadata.ipAddresses ?? [])].join(', ')
    : ''

  if (command !== 'build') {
    console.log(`[vite] Proxying frontend API traffic to ${apiOrigin}`)
    if (apiOriginSource === 'vmware' || apiOriginSource === 'vmware-fallback') {
      console.log('[vite] VMware guest backend auto-detection is active.')
    }
    if (apiOriginWarning) {
      console.warn(`[vite] ${apiOriginWarning}`)
    }
    console.log('[vite] Frontend dev server HTTPS is enabled with the LAN certificate bundle.')
    if (httpsHostsLabel) {
      console.log(`[vite] Frontend certificate SANs: ${httpsHostsLabel}`)
    }
  }

  return {
    plugins: [react()],
    test: {
      exclude: [...configDefaults.exclude, 'playwright/**'],
    },
    server: {
      host: true,
      https: httpsOptions,
      watch: {
        ignored: ['**/playwright-report/**', '**/test-results/**', '**/.playwright-artifacts/**'],
      },
      proxy: {
        '/api': {
          target: apiOrigin,
          changeOrigin: true,
          secure: false,
        },
        '/ws/chat': {
          target: chatOrigin,
          ws: true,
          changeOrigin: true,
          secure: false,
        },
      },
    },
    preview: {
      host: true,
      https: httpsOptions,
      proxy: {
        '/api': {
          target: apiOrigin,
          changeOrigin: true,
          secure: false,
        },
        '/ws/chat': {
          target: chatOrigin,
          ws: true,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
